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
    Task<UsuarioResponse?> RegistrarAsync(RegistrarUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse?> IniciarSesionAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<UsuarioResponse?> ObtenerPorIdAsync(ulong id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UsuarioResponse>> BuscarAsync(string? termino, CancellationToken cancellationToken = default);
    Task<ActualizarUsuarioResultado> ActualizarAsync(ulong id, ActualizarUsuarioRequest request, CancellationToken cancellationToken = default);
    Task<bool> DesactivarAsync(ulong id, CancellationToken cancellationToken = default);
}
