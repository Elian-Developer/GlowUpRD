using GlowUpRD.API.DTOs.Servicios;

namespace GlowUpRD.API.Services.Interfaces;

public interface IServicioService
{
    Task<MaintenanceResult<IReadOnlyList<ServicioResponse>>> BuscarAsync(long usuarioId, long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<ServicioResponse>> ObtenerAsync(long usuarioId, long id, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<ServicioResponse>> CrearAsync(long usuarioId, GuardarServicioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<ServicioResponse>> ActualizarAsync(long usuarioId, long id, GuardarServicioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<bool>> EliminarAsync(long usuarioId, long id, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<IReadOnlyList<CategoriaServicioResponse>>> ObtenerCategoriasAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
}
