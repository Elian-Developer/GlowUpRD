using GlowUpRD.API.DTOs.Reportes;

namespace GlowUpRD.API.Services.Interfaces;

public interface IReporteService
{
    Task<MaintenanceResult<ReporteResponse>> ObtenerAsync(long usuarioId, long negocioId, DateOnly desde, DateOnly hasta, long? sucursalId, CancellationToken cancellationToken = default);
}
