using GloupUpRD.API.DTOs.Reportes;
using GloupUpRD.API.Repositories.Interfaces;
using GloupUpRD.API.Services.Interfaces;

namespace GloupUpRD.API.Services.Implementations;

public sealed class ReporteService : IReporteService
{
    private const int SlotsPorDia = 16;

    private readonly ICitaRepository _citas;
    private readonly INegocioRepository _negocios;

    public ReporteService(ICitaRepository citas, INegocioRepository negocios)
    {
        _citas = citas;
        _negocios = negocios;
    }

    public async Task<MaintenanceResult<ReporteResponse>> ObtenerAsync(long usuarioId, long negocioId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default)
    {
        if (hasta < desde || hasta.DayNumber - desde.DayNumber > 62)
            return MaintenanceResult<ReporteResponse>.Fail(MaintenanceStatus.Invalid, "El rango debe estar entre 1 y 63 días.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<ReporteResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var citas = await _citas.BuscarAsync(negocioId, desde, hasta, null, null, cancellationToken);
        var activas = citas.Where(cita => cita.Estado != "cancelled").ToList();

        var ingresos = activas.Sum(cita => cita.Total);
        var confirmadas = activas.Count(cita => cita.Estado is "confirmed" or "completed");
        var tasaConfirmacion = activas.Count > 0 ? (int)Math.Round(confirmadas * 100m / activas.Count) : 0;

        var ocurrenciasPorDia = new Dictionary<DayOfWeek, int>();
        for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
            ocurrenciasPorDia[fecha.DayOfWeek] = ocurrenciasPorDia.GetValueOrDefault(fecha.DayOfWeek) + 1;

        var citasPorDia = activas.GroupBy(cita => cita.FechaCita.DayOfWeek).ToDictionary(grupo => grupo.Key, grupo => grupo.Count());

        var ocupacion = Enum.GetValues<DayOfWeek>().Select(dia =>
        {
            var capacidad = ocurrenciasPorDia.GetValueOrDefault(dia) * SlotsPorDia;
            var citasDelDia = citasPorDia.GetValueOrDefault(dia);
            var porcentaje = capacidad > 0 ? Math.Min(100, (int)Math.Round(citasDelDia * 100m / capacidad)) : 0;
            return new OcupacionDiaResponse((int)dia, porcentaje);
        }).ToList();

        return MaintenanceResult<ReporteResponse>.Ok(new ReporteResponse(ingresos, tasaConfirmacion, activas.Count, ocupacion));
    }
}
