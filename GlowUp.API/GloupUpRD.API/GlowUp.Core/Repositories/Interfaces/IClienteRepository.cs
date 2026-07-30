using GloupUpRD.API.Models;

namespace GloupUpRD.API.Repositories.Interfaces;

public interface IClienteRepository
{
    Task<List<ClientesNegocio>> BuscarAsync(long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default);
    Task<ClientesNegocio?> ObtenerAsync(long clienteId, bool tracking, CancellationToken cancellationToken = default);
    Task AgregarAsync(Cliente cliente, ClientesNegocio relacion, CancellationToken cancellationToken = default);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
