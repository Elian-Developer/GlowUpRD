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

    public async Task<MaintenanceResult<ReporteResponse>> ObtenerAsync(long usuarioId, long negocioId, DateOnly desde, DateOnly hasta, long? sucursalId, CancellationToken cancellationToken = default)
    {
        if (hasta < desde || hasta.DayNumber - desde.DayNumber > 62)
            return MaintenanceResult<ReporteResponse>.Fail(MaintenanceStatus.Invalid, "El rango debe estar entre 1 y 63 días.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<ReporteResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var negocio = await _negocios.ObtenerPerfilAsync(negocioId, cancellationToken);
        var sucursales = negocio?.Sucursales.Where(sucursal => sucursal.Estado == "active" && (!sucursalId.HasValue || sucursal.Id == sucursalId.Value)).ToList() ?? [];
        if (sucursales.Count == 0)
            return MaintenanceResult<ReporteResponse>.Fail(MaintenanceStatus.Invalid, "El negocio no tiene una sucursal principal configurada.");
        var empleadosActivos = await _citas.ObtenerEmpleadosAsync(negocioId, sucursalId, cancellationToken);
        var citas = await _citas.BuscarAsync(negocioId, desde, hasta, sucursalId, null, cancellationToken);
        var realizadas = citas.Where(cita => cita.Estado is "confirmed" or "completed").ToList();
        var festivosPorSucursal = negocio!.FeriadosNegocios.GroupBy(item => item.SucursalId)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Fecha).ToHashSet());

        var ingresos = realizadas.Sum(cita => cita.Total);
        var confirmadas = realizadas.Count;
        var noCanceladas = citas.Count(cita => cita.Estado != "cancelled");
        var tasaConfirmacion = noCanceladas > 0 ? (int)Math.Round(confirmadas * 100m / noCanceladas) : 0;

        var diasAbiertos = Enumerable.Range(0, hasta.DayNumber - desde.DayNumber + 1)
            .Select(offset => desde.AddDays(offset))
            .Where(fecha => sucursales.Any(sucursal => SucursalAbierta(sucursal, festivosPorSucursal, fecha)))
            .ToList();
        var minutosDisponiblesPorDia = diasAbiertos.ToDictionary(fecha => fecha, fecha =>
            sucursales.Where(sucursal => SucursalAbierta(sucursal, festivosPorSucursal, fecha)).Sum(sucursal =>
                empleadosActivos.Sum(empleado => MinutosDisponibles(empleado, HorarioDeSucursal(sucursal, fecha)!, fecha, sucursal.Id))));
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

        var empleados = empleadosActivos.Select(empleado =>
        {
            var citasEmpleado = realizadas.Where(cita => cita.EmpleadoId == empleado.Id).ToList();
            var capacidadEmpleado = diasAbiertos.Sum(fecha => sucursales
                .Where(sucursal => SucursalAbierta(sucursal, festivosPorSucursal, fecha))
                .Sum(sucursal => MinutosDisponibles(empleado, HorarioDeSucursal(sucursal, fecha)!, fecha, sucursal.Id)));
            var ocupacionEmpleado = capacidadEmpleado == 0 ? 0 : Math.Min(100, (int)Math.Round(citasEmpleado.Sum(MinutosBloqueados) * 100m / capacidadEmpleado));
            return new ReporteEmpleadoResponse(empleado.Id, $"{empleado.Nombre} {empleado.Apellido}", citasEmpleado.Count, citasEmpleado.Sum(cita => cita.Total), ocupacionEmpleado);
        })
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
        var capacidad = minutosDisponiblesPorDia.Values.Sum(value => value);
        var minutosOcupados = minutosOcupadosPorDia.Values.Sum(value => value);
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

    private static HorariosNegocio? HorarioDeSucursal(Sucursal sucursal, DateOnly fecha) =>
        sucursal.HorariosNegocios.SingleOrDefault(item => item.DiaSemana == (short)fecha.DayOfWeek);

    private static bool SucursalAbierta(Sucursal sucursal, IReadOnlyDictionary<long, HashSet<DateOnly>> festivosPorSucursal, DateOnly fecha)
    {
        var horario = HorarioDeSucursal(sucursal, fecha);
        return horario is not null && !horario.Cerrado && !festivosPorSucursal.GetValueOrDefault(sucursal.Id, []).Contains(fecha) &&
        horario.AbreA.HasValue && horario.CierraA.HasValue && horario.CierraA > horario.AbreA;
    }

    private static decimal MinutosDisponibles(Empleado empleado, HorariosNegocio horarioNegocio, DateOnly fecha, long sucursalId)
    {
        return empleado.HorariosEmpleados.Where(item => item.SucursalId == sucursalId && item.DiaSemana == (short)fecha.DayOfWeek && item.Activo).Sum(horarioEmpleado =>
        {
            var inicio = horarioNegocio.AbreA!.Value > horarioEmpleado.IniciaA ? horarioNegocio.AbreA.Value : horarioEmpleado.IniciaA;
            var fin = horarioNegocio.CierraA!.Value < horarioEmpleado.TerminaA ? horarioNegocio.CierraA.Value : horarioEmpleado.TerminaA;
            if (fin <= inicio) return 0;

            var inicioTurno = fecha.ToDateTime(inicio);
            var finTurno = fecha.ToDateTime(fin);
            var minutosAusentes = empleado.AusenciasEmpleados.Where(ausencia => ausencia.Estado == "scheduled" && ausencia.IniciaEn < finTurno && inicioTurno < ausencia.TerminaEn)
                .Sum(ausencia => (decimal)(
                    (ausencia.TerminaEn < finTurno ? ausencia.TerminaEn : finTurno) -
                    (ausencia.IniciaEn > inicioTurno ? ausencia.IniciaEn : inicioTurno)).TotalMinutes);
            return Math.Max(0, (decimal)(fin - inicio).TotalMinutes - minutosAusentes);
        });
    }

    private static decimal MinutosBloqueados(Cita cita) =>
        (decimal)(cita.Fin - cita.Inicio).TotalMinutes +
        cita.ServiciosCita.Sum(servicio => servicio.BufferAntesMinutos + servicio.BufferDespuesMinutos);
}
