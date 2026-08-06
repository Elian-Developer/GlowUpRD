using GlowUpRD.API.DTOs.Autenticacion;

namespace GlowUpRD.API.Services.Interfaces;

public enum ActualizarUsuarioEstado
{
    Exitoso,
    NoEncontrado,
    CorreoDuplicado
}

public sealed record ActualizarUsuarioResultado(
    ActualizarUsuarioEstado Estado,
    UsuarioResponse? Usuario = null);

public interface IAuthService
{
    Task<SesionAutenticada?> IniciarSesionAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> ObtenerPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ActualizarUsuarioResultado> ActualizarAsync(long id, ActualizarUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<bool> DesactivarAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> EliminarCuentaAsync(long id, CancellationToken cancellationToken = default);
    Task OlvidePasswordAsync(OlvidePasswordRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<bool>> RestablecerPasswordAsync(RestablecerPasswordRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<SesionAutenticada>> IniciarSesionConGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);
    Task<SesionAutenticada?> RefrescarSesionAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task CerrarSesionAsync(string? refreshToken, CancellationToken cancellationToken = default);
}
