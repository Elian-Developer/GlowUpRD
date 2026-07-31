using GlowUpRD.API.Data;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Repositories.Implementations;

public sealed class CitaRepository : ICitaRepository
{
    private readonly GlowUpDbContext _context;
    public CitaRepository(GlowUpDbContext context) => _context = context;

    public Task<bool> UsuarioTieneAccesoAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default) =>
        _context.Negocios.AnyAsync(negocio => negocio.Id == negocioId && negocio.Estado == "active" &&
            (negocio.UsuarioPropietarioId == usuarioId || negocio.MiembrosNegocios.Any(member => member.UsuarioId == usuarioId && member.Estado == "active")), cancellationToken);

    public async Task<List<Cita>> BuscarAsync(long negocioId, DateOnly desde, DateOnly hasta, long? sucursalId, long? empleadoId, CancellationToken cancellationToken = default)
    {
        var query = Detalles().Where(cita => cita.NegocioId == negocioId && cita.FechaCita >= desde && cita.FechaCita <= hasta);
        if (sucursalId.HasValue) query = query.Where(cita => cita.SucursalId == sucursalId.Value);
        if (empleadoId.HasValue) query = query.Where(cita => cita.EmpleadoId == empleadoId.Value);
        return await query.OrderBy(cita => cita.Inicio).ToListAsync(cancellationToken);
    }

    public Task<Cita?> ObtenerDetalleAsync(long id, CancellationToken cancellationToken = default) =>
        Detalles().FirstOrDefaultAsync(cita => cita.Id == id, cancellationToken);

    public Task<Cita?> ObtenerParaEditarAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Citas.Include(cita => cita.ServiciosCita).FirstOrDefaultAsync(cita => cita.Id == id, cancellationToken);

    public Task<List<Servicio>> ObtenerServiciosAsync(long negocioId, IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) =>
        _context.Servicios.AsNoTracking().Where(servicio => servicio.NegocioId == negocioId && ids.Contains(servicio.Id) && servicio.Activo).ToListAsync(cancellationToken);

    public Task<Empleado?> ObtenerEmpleadoAsync(long negocioId, long empleadoId, CancellationToken cancellationToken = default) =>
        _context.Empleados.AsNoTracking().FirstOrDefaultAsync(empleado => empleado.Id == empleadoId && empleado.NegocioId == negocioId && empleado.Estado == "active", cancellationToken);

    public Task<Sucursal?> ObtenerSucursalAsync(long negocioId, long sucursalId, CancellationToken cancellationToken = default) =>
        _context.Sucursales.AsNoTracking().FirstOrDefaultAsync(sucursal => sucursal.Id == sucursalId && sucursal.NegocioId == negocioId && sucursal.Estado == "active", cancellationToken);

    public Task<Cliente?> ObtenerClienteAsync(long clienteId, CancellationToken cancellationToken = default) =>
        _context.Clientes.AsNoTracking().FirstOrDefaultAsync(cliente => cliente.Id == clienteId, cancellationToken);

    public Task<ClientesNegocio?> ObtenerClienteNegocioAsync(long negocioId, long clienteId, CancellationToken cancellationToken = default) =>
        _context.ClientesNegocios.AsNoTracking().FirstOrDefaultAsync(item => item.NegocioId == negocioId && item.ClienteId == clienteId && item.Estado == "active", cancellationToken);

    public Task<bool> ExisteConflictoAsync(long empleadoId, DateTime inicio, DateTime fin, long? excluirCitaId, CancellationToken cancellationToken = default) =>
        _context.Citas.AnyAsync(cita => cita.EmpleadoId == empleadoId &&
            cita.Estado != "cancelled" && cita.Estado != "no_show" &&
            (!excluirCitaId.HasValue || cita.Id != excluirCitaId.Value) &&
            cita.Inicio < fin && cita.Fin > inicio, cancellationToken);

    public Task<List<Sucursal>> ObtenerSucursalesAsync(long negocioId, CancellationToken cancellationToken = default) =>
        _context.Sucursales.AsNoTracking().Where(item => item.NegocioId == negocioId && item.Estado == "active").OrderBy(item => item.Nombre).ToListAsync(cancellationToken);

    public Task<List<ClientesNegocio>> ObtenerClientesAsync(long negocioId, CancellationToken cancellationToken = default) =>
        _context.ClientesNegocios.AsNoTracking().Include(item => item.Cliente).Where(item => item.NegocioId == negocioId && item.Estado == "active").OrderBy(item => item.Cliente.Nombre).ToListAsync(cancellationToken);

    public Task<List<Empleado>> ObtenerEmpleadosAsync(long negocioId, CancellationToken cancellationToken = default) =>
        _context.Empleados.AsNoTracking().Where(item => item.NegocioId == negocioId && item.Estado == "active").OrderBy(item => item.Nombre).ToListAsync(cancellationToken);

    public Task<List<Servicio>> ObtenerServiciosActivosAsync(long negocioId, CancellationToken cancellationToken = default) =>
        _context.Servicios.AsNoTracking().Where(item => item.NegocioId == negocioId && item.Activo).OrderBy(item => item.Nombre).ToListAsync(cancellationToken);

    public Task<List<Negocio>> ObtenerNegociosAsync(long usuarioId, CancellationToken cancellationToken = default) =>
        _context.Negocios.AsNoTracking().Where(item => item.Estado == "active" &&
            (item.UsuarioPropietarioId == usuarioId || item.MiembrosNegocios.Any(member => member.UsuarioId == usuarioId && member.Estado == "active")))
            .OrderBy(item => item.Nombre).ToListAsync(cancellationToken);

    public Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default) => _context.Citas.AddAsync(cita, cancellationToken).AsTask();

    public void ReemplazarServicios(Cita cita, IEnumerable<ServicioCita> servicios)
    {
        _context.ServiciosCita.RemoveRange(cita.ServiciosCita);
        cita.ServiciosCita.Clear();
        foreach (var servicio in servicios) cita.ServiciosCita.Add(servicio);
    }

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);

    private IQueryable<Cita> Detalles() => _context.Citas.AsNoTracking()
        .Include(cita => cita.Sucursal).Include(cita => cita.Cliente).Include(cita => cita.Empleado)
        .Include(cita => cita.ServiciosCita).ThenInclude(item => item.Servicio);
}
