using GlowUpRD.API.DTOs.Ausencias;

namespace GlowUpRD.API.Services.Interfaces;

public interface IAusenciaService
{
    Task<MaintenanceResult<IReadOnlyList<AusenciaResponse>>> BuscarAsync(long usuarioId, long negocioId, DateOnly desde, DateOnly hasta, long? empleadoId, bool incluirCanceladas, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<AusenciaResponse>> CrearAsync(long usuarioId, GuardarAusenciaRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<AusenciaResponse>> ActualizarAsync(long usuarioId, long id, GuardarAusenciaRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<bool>> CancelarAsync(long usuarioId, long id, CancellationToken cancellationToken = default);
}
