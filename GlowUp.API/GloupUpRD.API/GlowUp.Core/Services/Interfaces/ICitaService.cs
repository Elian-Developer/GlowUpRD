using GloupUpRD.API.DTOs.Citas;

namespace GloupUpRD.API.Services.Interfaces;

public interface ICitaService
{
    Task<MaintenanceResult<IReadOnlyList<CitaResponse>>> BuscarAsync(ulong usuarioId, ulong negocioId, DateOnly desde, DateOnly hasta, ulong? sucursalId, ulong? empleadoId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<CitaResponse>> ObtenerAsync(ulong usuarioId, ulong id, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<CitaResponse>> CrearAsync(ulong usuarioId, GuardarCitaRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<CitaResponse>> ActualizarAsync(ulong usuarioId, ulong id, GuardarCitaRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<bool>> EliminarAsync(ulong usuarioId, ulong id, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<CatalogoCitasResponse>> ObtenerCatalogosAsync(ulong usuarioId, ulong negocioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NegocioResumenResponse>> ObtenerNegociosAsync(ulong usuarioId, CancellationToken cancellationToken = default);
}
