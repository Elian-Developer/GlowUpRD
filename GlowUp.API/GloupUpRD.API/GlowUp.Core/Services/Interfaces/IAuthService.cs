using GloupUpRD.API.DTOs.Autenticacion;

namespace GloupUpRD.API.Services.Interfaces;

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
    Task<LoginResponse?> IniciarSesionAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> ObtenerPorIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ActualizarUsuarioResultado> ActualizarAsync(long id, ActualizarUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<bool> DesactivarAsync(long id, CancellationToken cancellationToken = default);
    Task OlvidePasswordAsync(OlvidePasswordRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<bool>> RestablecerPasswordAsync(RestablecerPasswordRequest request, CancellationToken cancellationToken = default);
    Task<MaintenanceResult<LoginResponse>> IniciarSesionConGoogleAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default);
}
