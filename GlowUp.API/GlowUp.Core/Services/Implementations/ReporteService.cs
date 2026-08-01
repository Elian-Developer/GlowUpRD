using GlowUpRD.API.DTOs.Reportes;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using GlowUpRD.API.Services.Interfaces;

namespace GlowUpRD.API.Services.Implementations;

public sealed class ReporteService : IReporteService
{
    private readonly ICitaRepository _citas;
    private readonly IClienteRepository _clientes;
    private readonly INegocioRepository _negocios;

    public ReporteService(ICitaRepository citas, IClienteRepository clientes, INegocioRepository negocios)
    {
        _citas = citas;
        _clientes = clientes;
        _negocios = negocios;
    }

    public async Task<MaintenanceResult<ReporteResponse>> ObtenerAsync(long usuarioId, long negocioId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default)
    {
        if (hasta < desde || hasta.DayNumber - desde.DayNumber > 62)
            return MaintenanceResult<ReporteResponse>.Fail(MaintenanceStatus.Invalid, "El rango debe estar entre 1 y 63 días.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<ReporteResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var negocio = await _negocios.ObtenerPerfilAsync(negocioId, cancellationToken);
        var sucursalPrincipal = negocio?.Sucursales.FirstOrDefault(sucursal => sucursal.EsPrincipal);
        if (sucursalPrincipal is null)
            return MaintenanceResult<ReporteResponse>.Fail(MaintenanceStatus.Invalid, "El negocio no tiene una sucursal principal configurada.");

        var horariosPorDia = sucursalPrincipal.HorariosNegocios.ToDictionary(item => item.DiaSemana);
        var empleadosActivos = await _citas.ObtenerEmpleadosAsync(negocioId, cancellationToken);
        var citas = await _citas.BuscarAsync(negocioId, desde, hasta, null, null, cancellationToken);
        var realizadas = citas.Where(cita => cita.Estado is "confirmed" or "completed").ToList();

        var ingresos = realizadas.Sum(cita => cita.Total);
        var confirmadas = realizadas.Count;
        var noCanceladas = citas.Count(cita => cita.Estado != "cancelled");
        var tasaConfirmacion = noCanceladas > 0 ? (int)Math.Round(confirmadas * 100m / noCanceladas) : 0;

        var diasAbiertos = Enumerable.Range(0, hasta.DayNumber - desde.DayNumber + 1)
            .Select(offset => desde.AddDays(offset))
            .Where(fecha => HorarioAbierto(horariosPorDia, fecha))
            .ToList();
        var minutosDisponiblesPorDia = diasAbiertos.ToDictionary(fecha => fecha, fecha =>
            MinutosDisponibles(horariosPorDia[(short)fecha.DayOfWeek], empleadosActivos.Count));
        var minutosAbiertosPorEmpleado = diasAbiertos.Sum(fecha =>
            (decimal)(horariosPorDia[(short)fecha.DayOfWeek].CierraA!.Value - horariosPorDia[(short)fecha.DayOfWeek].AbreA!.Value).TotalMinutes);
        var minutosOcupadosPorDia = realizadas.Where(cita => minutosDisponiblesPorDia.ContainsKey(cita.FechaCita))
            .GroupBy(cita => cita.FechaCita)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(MinutosBloqueados));

        var ocupacion = Enum.GetValues<DayOfWeek>().Select(dia =>
        {
            var capacidad = minutosDisponiblesPorDia.Where(item => item.Key.DayOfWeek == dia).Sum(item => item.Value);
            var minutosOcupados = minutosOcupadosPorDia.Where(item => item.Key.DayOfWeek == dia).Sum(item => item.Value);
            var porcentaje = capacidad > 0 ? Math.Min(100, (int)Math.Round(minutosOcupados * 100m / capacidad)) : 0;
            return new OcupacionDiaResponse((int)dia, porcentaje);
        }).ToList();

        var evolucion = diasAbiertos.Select(fecha =>
        {
            var citasDelDia = realizadas.Where(cita => cita.FechaCita == fecha).ToList();
            return new ReporteDiaResponse(fecha, citasDelDia.Count, citasDelDia.Sum(cita => cita.Total));
        }).ToList();

        var empleados = realizadas.GroupBy(cita => new { cita.EmpleadoId, Nombre = $"{cita.Empleado.Nombre} {cita.Empleado.Apellido}" })
            .Select(grupo => new ReporteEmpleadoResponse(
                grupo.Key.EmpleadoId, grupo.Key.Nombre, grupo.Count(), grupo.Sum(cita => cita.Total),
                minutosAbiertosPorEmpleado == 0 ? 0 : Math.Min(100, (int)Math.Round(grupo.Sum(MinutosBloqueados) * 100m / minutosAbiertosPorEmpleado))))
            .OrderByDescending(item => item.Ingresos).ThenBy(item => item.Nombre).ToList();

        var servicios = realizadas.SelectMany(cita => cita.ServiciosCita)
            .GroupBy(servicio => servicio.NombreServicio)
            .Select(grupo => new ReporteServicioResponse(grupo.Key, grupo.Count(), grupo.Sum(servicio => servicio.Precio)))
            .OrderByDescending(item => item.Ingresos).ThenBy(item => item.Nombre).ToList();

        var estados = citas.GroupBy(cita => cita.Estado)
            .Select(grupo => new ReporteEstadoResponse(grupo.Key, grupo.Count()))
            .OrderByDescending(item => item.Cantidad).ToList();

        var clientesAtendidos = realizadas.Select(cita => cita.ClienteId).Distinct().ToHashSet();
        var clientesHistoricos = (await _citas.ObtenerClientesConIngresosAntesDeAsync(negocioId, desde, cancellationToken)).ToHashSet();
        var tasaRetencion = clientesAtendidos.Count == 0 ? 0 :
            (int)Math.Round(clientesAtendidos.Count(clienteId => clientesHistoricos.Contains(clienteId)) * 100m / clientesAtendidos.Count);
        var clientesNuevos = await _clientes.ContarNuevosAsync(negocioId, desde, hasta, cancellationToken);
        var capacidad = minutosDisponiblesPorDia.Values.Sum();
        var minutosOcupados = minutosOcupadosPorDia.Values.Sum();
        var ocupacionAgenda = capacidad == 0 ? 0 : Math.Min(100, (int)Math.Round(minutosOcupados * 100m / capacidad));
        var tasaCancelacion = citas.Count == 0 ? 0 : (int)Math.Round(citas.Count(cita => cita.Estado == "cancelled") * 100m / citas.Count);
        var tasaNoConfirmadas = citas.Count == 0 ? 0 : (int)Math.Round(citas.Count(cita => cita.Estado == "pending") * 100m / citas.Count);

        return MaintenanceResult<ReporteResponse>.Ok(new ReporteResponse(
            ingresos, tasaConfirmacion, realizadas.Count, ocupacion,
            citas.Count, realizadas.Count(cita => cita.Estado == "completed"), citas.Count(cita => cita.Estado == "pending"),
            citas.Count(cita => cita.Estado == "cancelled"), clientesAtendidos.Count,
            realizadas.Count == 0 ? 0 : decimal.Round(ingresos / realizadas.Count, 2), clientesNuevos, tasaRetencion,
            ocupacionAgenda, tasaCancelacion, tasaNoConfirmadas, evolucion, empleados, servicios, estados));
    }

    private static bool HorarioAbierto(IReadOnlyDictionary<short, HorariosNegocio> horariosPorDia, DateOnly fecha) =>
        horariosPorDia.TryGetValue((short)fecha.DayOfWeek, out var horario) && !horario.Cerrado &&
        horario.AbreA.HasValue && horario.CierraA.HasValue && horario.CierraA > horario.AbreA;

    private static decimal MinutosDisponibles(HorariosNegocio horario, int empleadosActivos) =>
        empleadosActivos == 0 ? 0 : (decimal)(horario.CierraA!.Value - horario.AbreA!.Value).TotalMinutes * empleadosActivos;

    private static decimal MinutosBloqueados(Cita cita) =>
        (decimal)(cita.Fin - cita.Inicio).TotalMinutes +
        cita.ServiciosCita.Sum(servicio => servicio.Servicio.BufferAntesMinutos + servicio.Servicio.BufferDespuesMinutos);
}
