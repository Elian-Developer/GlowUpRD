using GloupUpRD.API.DTOs.Reportes;

namespace GloupUpRD.API.Services.Interfaces;

public interface IReporteService
{
    Task<MaintenanceResult<ReporteResponse>> ObtenerAsync(long usuarioId, long negocioId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default);
}
