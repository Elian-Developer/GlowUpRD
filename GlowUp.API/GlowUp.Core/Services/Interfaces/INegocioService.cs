using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.DTOs.Negocios;

namespace GlowUpRD.API.Services.Interfaces;

public interface INegocioService
{
    Task<MaintenanceResult<LoginResponse>> RegistrarAsync(RegistrarNegocioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<NegocioDetalleResponse>> ObtenerAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<NegocioDetalleResponse>> ActualizarAsync(long usuarioId, long negocioId, ActualizarNegocioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<IReadOnlyList<MiembroNegocioResponse>>> ObtenerMiembrosAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<MiembroNegocioResponse>> CrearUsuarioAsync(long usuarioId, long negocioId, CrearUsuarioNegocioRequest request, CancellationToken cancellationToken = default);
}
