using GloupUpRD.API.Models;

namespace GloupUpRD.API.Repositories.Interfaces;

public interface IServicioRepository
{
    Task<bool> UsuarioTieneAccesoAsync(ulong usuarioId, ulong negocioId, CancellationToken cancellationToken = default);
    Task<List<Service>> BuscarAsync(ulong negocioId, bool incluirInactivos, CancellationToken cancellationToken = default);
    Task<Service?> ObtenerAsync(ulong id, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> CategoriaValidaAsync(ulong negocioId, ulong categoriaId, CancellationToken cancellationToken = default);
    Task<bool> NombreDuplicadoAsync(ulong negocioId, string nombre, ulong? excluirId, CancellationToken cancellationToken = default);
    Task<List<ServiceCategory>> ObtenerCategoriasAsync(ulong negocioId, CancellationToken cancellationToken = default);
    Task AgregarAsync(Service servicio, CancellationToken cancellationToken = default);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
