using GlowUpRD.API.DTOs.Clientes;

namespace GlowUpRD.API.Services.Interfaces;

public interface IClienteService
{
    Task<MaintenanceResult<IReadOnlyList<ClienteResponse>>> BuscarAsync(long usuarioId, long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<ClienteResponse>> ObtenerAsync(long usuarioId, long id, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<ClienteResponse>> CrearAsync(long usuarioId, GuardarClienteRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<ClienteResponse>> ActualizarAsync(long usuarioId, long id, GuardarClienteRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<bool>> EliminarAsync(long usuarioId, long id, CancellationToken cancellationToken = default);
}
