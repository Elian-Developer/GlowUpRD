using GloupUpRD.API.DTOs.Autenticacion;
using GloupUpRD.API.DTOs.Negocios;

namespace GloupUpRD.API.Services.Interfaces;

public interface INegocioService
{
    Task<MaintenanceResult<LoginResponse>> RegistrarAsync(RegistrarNegocioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<NegocioDetalleResponse>> ObtenerAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<NegocioDetalleResponse>> ActualizarAsync(long usuarioId, long negocioId, ActualizarNegocioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<IReadOnlyList<MiembroNegocioResponse>>> ObtenerMiembrosAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<MiembroNegocioResponse>> CrearUsuarioAsync(long usuarioId, long negocioId, CrearUsuarioNegocioRequest request, CancellationToken cancellationToken = default);
}
