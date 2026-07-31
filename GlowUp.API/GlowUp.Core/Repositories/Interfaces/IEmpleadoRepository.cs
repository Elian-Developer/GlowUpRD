using GlowUpRD.API.Models;

namespace GlowUpRD.API.Repositories.Interfaces;

public interface IEmpleadoRepository
{
    Task<List<Empleado>> BuscarAsync(long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default);
    Task<Empleado?> ObtenerAsync(long id, bool tracking, CancellationToken cancellationToken = default);
    Task<bool> SucursalValidaAsync(long negocioId, long sucursalId, CancellationToken cancellationToken = default);
    Task AgregarAsync(Empleado empleado, CancellationToken cancellationToken = default);
    Task AgregarConUsuarioAsync(Empleado empleado, Usuario usuario, MiembrosNegocio miembro, CancellationToken cancellationToken = default);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
