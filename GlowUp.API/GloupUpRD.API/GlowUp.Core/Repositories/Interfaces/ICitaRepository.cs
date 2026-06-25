using GloupUpRD.API.Models;

namespace GloupUpRD.API.Repositories.Interfaces;

public interface ICitaRepository
{
    Task<bool> UsuarioTieneAccesoAsync(ulong usuarioId, ulong negocioId, CancellationToken cancellationToken = default);
    Task<List<Appointment>> BuscarAsync(ulong negocioId, DateOnly desde, DateOnly hasta, ulong? sucursalId, ulong? empleadoId, CancellationToken cancellationToken = default);
    Task<Appointment?> ObtenerDetalleAsync(ulong id, CancellationToken cancellationToken = default);
    Task<Appointment?> ObtenerParaEditarAsync(ulong id, CancellationToken cancellationToken = default);
    Task<List<Service>> ObtenerServiciosAsync(ulong negocioId, IReadOnlyCollection<ulong> ids, CancellationToken cancellationToken = default);
    Task<Employee?> ObtenerEmpleadoAsync(ulong negocioId, ulong empleadoId, CancellationToken cancellationToken = default);
    Task<Branch?> ObtenerSucursalAsync(ulong negocioId, ulong sucursalId, CancellationToken cancellationToken = default);
    Task<Customer?> ObtenerClienteAsync(ulong clienteId, CancellationToken cancellationToken = default);
    Task<BusinessCustomer?> ObtenerClienteNegocioAsync(ulong negocioId, ulong clienteId, CancellationToken cancellationToken = default);
    Task<bool> ExisteConflictoAsync(ulong empleadoId, DateTime inicio, DateTime fin, ulong? excluirCitaId, CancellationToken cancellationToken = default);
    Task<List<Branch>> ObtenerSucursalesAsync(ulong negocioId, CancellationToken cancellationToken = default);
    Task<List<BusinessCustomer>> ObtenerClientesAsync(ulong negocioId, CancellationToken cancellationToken = default);
    Task<List<Employee>> ObtenerEmpleadosAsync(ulong negocioId, CancellationToken cancellationToken = default);
    Task<List<Service>> ObtenerServiciosActivosAsync(ulong negocioId, CancellationToken cancellationToken = default);
    Task<List<Business>> ObtenerNegociosAsync(ulong usuarioId, CancellationToken cancellationToken = default);
    Task AgregarAsync(Appointment cita, CancellationToken cancellationToken = default);
    void ReemplazarServicios(Appointment cita, IEnumerable<AppointmentService> servicios);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
