using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.DTOs.Negocios;

namespace GlowUpRD.API.Services.Interfaces;

public interface INegocioService
{
    Task<MaintenanceResult<SesionAutenticada>> RegistrarAsync(RegistrarNegocioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<NegocioDetalleResponse>> ObtenerAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<NegocioDetalleResponse>> ActualizarAsync(long usuarioId, long negocioId, ActualizarNegocioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<IReadOnlyList<MiembroNegocioResponse>>> ObtenerMiembrosAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<MiembroNegocioResponse>> CrearUsuarioAsync(long usuarioId, long negocioId, CrearUsuarioNegocioRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<IReadOnlyList<SucursalResumenResponse>>> ObtenerSucursalesAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<SucursalDetalleResponse>> ObtenerSucursalAsync(long usuarioId, long negocioId, long sucursalId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<SucursalDetalleResponse>> CrearSucursalAsync(long usuarioId, long negocioId, GuardarSucursalRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<SucursalDetalleResponse>> ActualizarSucursalAsync(long usuarioId, long negocioId, long sucursalId, GuardarSucursalRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<bool>> DesactivarSucursalAsync(long usuarioId, long negocioId, long sucursalId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<SucursalDetalleResponse>> ReactivarSucursalAsync(long usuarioId, long negocioId, long sucursalId, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<SucursalDetalleResponse>> MarcarSucursalPrincipalAsync(long usuarioId, long negocioId, long sucursalId, CancellationToken cancellationToken = default);
}
