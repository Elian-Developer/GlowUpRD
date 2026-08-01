using GlowUpRD.API.Models;

namespace GlowUpRD.API.Repositories.Interfaces;

public interface ICitaRepository
{
    Task<bool> UsuarioTieneAccesoAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default);
    Task<List<Cita>> BuscarAsync(long negocioId, DateOnly desde, DateOnly hasta, long? sucursalId, long? empleadoId, CancellationToken cancellationToken = default);
    Task<List<long>> ObtenerClientesConIngresosAntesDeAsync(long negocioId, DateOnly fecha, CancellationToken cancellationToken = default);
    Task<Cita?> ObtenerDetalleAsync(long id, CancellationToken cancellationToken = default);
    Task<Cita?> ObtenerParaEditarAsync(long id, CancellationToken cancellationToken = default);
    Task<List<Servicio>> ObtenerServiciosAsync(long negocioId, IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default);
    Task<Empleado?> ObtenerEmpleadoAsync(long negocioId, long empleadoId, CancellationToken cancellationToken = default);
    Task<Sucursal?> ObtenerSucursalAsync(long negocioId, long sucursalId, CancellationToken cancellationToken = default);
    Task<Cliente?> ObtenerClienteAsync(long clienteId, CancellationToken cancellationToken = default);
    Task<ClientesNegocio?> ObtenerClienteNegocioAsync(long negocioId, long clienteId, CancellationToken cancellationToken = default);
    Task<List<Cita>> ObtenerCitasParaConflictoAsync(long empleadoId, DateTime inicio, DateTime fin, long? excluirCitaId, CancellationToken cancellationToken = default);
    Task<List<Sucursal>> ObtenerSucursalesAsync(long negocioId, CancellationToken cancellationToken = default);
    Task<List<ClientesNegocio>> ObtenerClientesAsync(long negocioId, CancellationToken cancellationToken = default);
    Task<List<Empleado>> ObtenerEmpleadosAsync(long negocioId, CancellationToken cancellationToken = default);
    Task<List<Servicio>> ObtenerServiciosActivosAsync(long negocioId, CancellationToken cancellationToken = default);
    Task<List<Negocio>> ObtenerNegociosAsync(long usuarioId, CancellationToken cancellationToken = default);
    Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default);
    void ReemplazarServicios(Cita cita, IEnumerable<ServicioCita> servicios);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
