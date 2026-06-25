using GloupUpRD.API.DTOs.Servicios;

namespace GloupUpRD.API.Services.Interfaces;

public interface IServicioService
{
    Task<MaintenanceResult<IReadOnlyList<ServicioResponse>>> BuscarAsync(ulong usuarioId, ulong negocioId, bool incluirInactivos, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<ServicioResponse>> ObtenerAsync(ulong usuarioId, ulong id, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<ServicioResponse>> CrearAsync(ulong usuarioId, GuardarServicioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<ServicioResponse>> ActualizarAsync(ulong usuarioId, ulong id, GuardarServicioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<bool>> EliminarAsync(ulong usuarioId, ulong id, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<IReadOnlyList<CategoriaServicioResponse>>> ObtenerCategoriasAsync(ulong usuarioId, ulong negocioId, CancellationToken cancellationToken = default);
}
