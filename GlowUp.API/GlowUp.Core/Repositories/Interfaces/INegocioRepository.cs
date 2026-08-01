using GlowUpRD.API.Models;

namespace GlowUpRD.API.Repositories.Interfaces;

public interface INegocioRepository
{
    Task<bool> SlugExisteAsync(string slug, CancellationToken cancellationToken = default);
    Task<Negocio?> ObtenerAsync(long id, CancellationToken cancellationToken = default);
    Task<Negocio?> ObtenerParaEditarAsync(long id, CancellationToken cancellationToken = default);
    Task<Negocio?> ObtenerPerfilAsync(long id, CancellationToken cancellationToken = default);
    Task<Negocio?> ObtenerPerfilParaEditarAsync(long id, CancellationToken cancellationToken = default);
    Task<bool> UsuarioEsPropietarioAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<bool> UsuarioTieneAccesoAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<List<MiembrosNegocio>> ObtenerMiembrosAsync(long negocioId, CancellationToken cancellationToken = default);
    Task RegistrarAsync(Negocio negocio, Usuario propietario, MiembrosNegocio miembro, CancellationToken cancellationToken = default);
    Task AgregarMiembroAsync(Usuario usuario, MiembrosNegocio miembro, CancellationToken cancellationToken = default);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
