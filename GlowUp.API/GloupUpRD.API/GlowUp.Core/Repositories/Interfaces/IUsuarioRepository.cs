using GloupUpRD.API.Models;

namespace GloupUpRD.API.Repositories.Interfaces;

public interface IUsuarioRepository
{
    Task<User?> ObtenerPorIdAsync(ulong id, CancellationToken cancellationToken = default);
    Task<User?> ObtenerPorCorreoAsync(string correo, CancellationToken cancellationToken = default);
    Task<List<User>> BuscarAsync(string? termino, CancellationToken cancellationToken = default);
    Task AgregarAsync(User usuario, CancellationToken cancellationToken = default);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
