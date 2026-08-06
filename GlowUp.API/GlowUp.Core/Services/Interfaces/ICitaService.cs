using GlowUpRD.API.DTOs.Citas;

namespace GlowUpRD.API.Services.Interfaces;

public interface ICitaService
{
    Task<MaintenanceResult<IReadOnlyList<CitaResponse>>> BuscarAsync(long usuarioId, long negocioId, DateOnly desde, DateOnly hasta, long? sucursalId, long? empleadoId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<CitaResponse>> ObtenerAsync(long usuarioId, long id, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<CitaResponse>> CrearAsync(long usuarioId, GuardarCitaRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<CitaResponse>> ActualizarAsync(long usuarioId, long id, GuardarCitaRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<bool>> EliminarAsync(long usuarioId, long id, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<CatalogoCitasResponse>> ObtenerCatalogosAsync(long usuarioId, long negocioId, long? sucursalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NegocioResumenResponse>> ObtenerNegociosAsync(long usuarioId, CancellationToken cancellationToken = default);
}
