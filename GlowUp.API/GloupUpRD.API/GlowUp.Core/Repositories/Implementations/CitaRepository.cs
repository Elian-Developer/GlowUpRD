using GloupUpRD.API.Data;
using GloupUpRD.API.Models;
using GloupUpRD.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloupUpRD.API.Repositories.Implementations;

public sealed class CitaRepository : ICitaRepository
{
    private readonly GlowUpDbContext _context;
    public CitaRepository(GlowUpDbContext context) => _context = context;

    public Task<bool> UsuarioTieneAccesoAsync(ulong usuarioId, ulong negocioId, CancellationToken cancellationToken = default) =>
        _context.Businesses.AnyAsync(business => business.Id == negocioId && business.Status == "active" &&
            (business.OwnerUserId == usuarioId || business.BusinessMembers.Any(member => member.UserId == usuarioId && member.Status == "active")), cancellationToken);

    public async Task<List<Appointment>> BuscarAsync(ulong negocioId, DateOnly desde, DateOnly hasta, ulong? sucursalId, ulong? empleadoId, CancellationToken cancellationToken = default)
    {
        var query = Detalles().Where(cita => cita.BusinessId == negocioId && cita.AppointmentDate >= desde && cita.AppointmentDate <= hasta);
        if (sucursalId.HasValue) query = query.Where(cita => cita.BranchId == sucursalId.Value);
        if (empleadoId.HasValue) query = query.Where(cita => cita.EmployeeId == empleadoId.Value);
        return await query.OrderBy(cita => cita.StartsAt).ToListAsync(cancellationToken);
    }

    public Task<Appointment?> ObtenerDetalleAsync(ulong id, CancellationToken cancellationToken = default) =>
        Detalles().FirstOrDefaultAsync(cita => cita.Id == id, cancellationToken);

    public Task<Appointment?> ObtenerParaEditarAsync(ulong id, CancellationToken cancellationToken = default) =>
        _context.Appointments.Include(cita => cita.AppointmentServices).FirstOrDefaultAsync(cita => cita.Id == id, cancellationToken);

    public Task<List<Service>> ObtenerServiciosAsync(ulong negocioId, IReadOnlyCollection<ulong> ids, CancellationToken cancellationToken = default) =>
        _context.Services.AsNoTracking().Where(service => service.BusinessId == negocioId && ids.Contains(service.Id) && service.IsActive != false).ToListAsync(cancellationToken);

    public Task<Employee?> ObtenerEmpleadoAsync(ulong negocioId, ulong empleadoId, CancellationToken cancellationToken = default) =>
        _context.Employees.AsNoTracking().FirstOrDefaultAsync(employee => employee.Id == empleadoId && employee.BusinessId == negocioId && employee.Status == "active", cancellationToken);

    public Task<Branch?> ObtenerSucursalAsync(ulong negocioId, ulong sucursalId, CancellationToken cancellationToken = default) =>
        _context.Branches.AsNoTracking().FirstOrDefaultAsync(branch => branch.Id == sucursalId && branch.BusinessId == negocioId && branch.Status == "active", cancellationToken);

    public Task<Customer?> ObtenerClienteAsync(ulong clienteId, CancellationToken cancellationToken = default) =>
        _context.Customers.AsNoTracking().FirstOrDefaultAsync(customer => customer.Id == clienteId, cancellationToken);

    public Task<BusinessCustomer?> ObtenerClienteNegocioAsync(ulong negocioId, ulong clienteId, CancellationToken cancellationToken = default) =>
        _context.BusinessCustomers.AsNoTracking().FirstOrDefaultAsync(item => item.BusinessId == negocioId && item.CustomerId == clienteId && item.Status == "active", cancellationToken);

    public Task<bool> ExisteConflictoAsync(ulong empleadoId, DateTime inicio, DateTime fin, ulong? excluirCitaId, CancellationToken cancellationToken = default) =>
        _context.Appointments.AnyAsync(cita => cita.EmployeeId == empleadoId &&
            cita.Status != "cancelled" && cita.Status != "no_show" &&
            (!excluirCitaId.HasValue || cita.Id != excluirCitaId.Value) &&
            cita.StartsAt < fin && cita.EndsAt > inicio, cancellationToken);

    public Task<List<Branch>> ObtenerSucursalesAsync(ulong negocioId, CancellationToken cancellationToken = default) =>
        _context.Branches.AsNoTracking().Where(item => item.BusinessId == negocioId && item.Status == "active").OrderBy(item => item.Name).ToListAsync(cancellationToken);

    public Task<List<BusinessCustomer>> ObtenerClientesAsync(ulong negocioId, CancellationToken cancellationToken = default) =>
        _context.BusinessCustomers.AsNoTracking().Include(item => item.Customer).Where(item => item.BusinessId == negocioId && item.Status == "active").OrderBy(item => item.Customer.FirstName).ToListAsync(cancellationToken);

    public Task<List<Employee>> ObtenerEmpleadosAsync(ulong negocioId, CancellationToken cancellationToken = default) =>
        _context.Employees.AsNoTracking().Where(item => item.BusinessId == negocioId && item.Status == "active").OrderBy(item => item.FirstName).ToListAsync(cancellationToken);

    public Task<List<Service>> ObtenerServiciosActivosAsync(ulong negocioId, CancellationToken cancellationToken = default) =>
        _context.Services.AsNoTracking().Where(item => item.BusinessId == negocioId && item.IsActive != false).OrderBy(item => item.Name).ToListAsync(cancellationToken);

    public Task<List<Business>> ObtenerNegociosAsync(ulong usuarioId, CancellationToken cancellationToken = default) =>
        _context.Businesses.AsNoTracking().Where(item => item.Status == "active" &&
            (item.OwnerUserId == usuarioId || item.BusinessMembers.Any(member => member.UserId == usuarioId && member.Status == "active")))
            .OrderBy(item => item.Name).ToListAsync(cancellationToken);

    public Task AgregarAsync(Appointment cita, CancellationToken cancellationToken = default) => _context.Appointments.AddAsync(cita, cancellationToken).AsTask();

    public void ReemplazarServicios(Appointment cita, IEnumerable<AppointmentService> servicios)
    {
        _context.AppointmentServices.RemoveRange(cita.AppointmentServices);
        cita.AppointmentServices.Clear();
        foreach (var servicio in servicios) cita.AppointmentServices.Add(servicio);
    }

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);

    private IQueryable<Appointment> Detalles() => _context.Appointments.AsNoTracking()
        .Include(cita => cita.Branch).Include(cita => cita.Customer).Include(cita => cita.Employee)
        .Include(cita => cita.AppointmentServices).ThenInclude(item => item.Service);
}
