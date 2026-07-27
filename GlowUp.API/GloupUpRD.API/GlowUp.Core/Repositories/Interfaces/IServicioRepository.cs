using GloupUpRD.API.Models;

namespace GloupUpRD.API.Repositories.Interfaces;

public interface IServicioRepository
{
    Task<bool> UsuarioTieneAccesoAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<List<Servicio>> BuscarAsync(long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default);
    Task<Servicio?> ObtenerAsync(long id, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> CategoriaValidaAsync(long negocioId, long categoriaId, CancellationToken cancellationToken = default);
    Task<bool> NombreDuplicadoAsync(long negocioId, string nombre, long? excluirId, CancellationToken cancellationToken = default);
    Task<List<CategoriasServicio>> ObtenerCategoriasAsync(long negocioId, CancellationToken cancellationToken = default);
    Task AgregarAsync(Servicio servicio, CancellationToken cancellationToken = default);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
