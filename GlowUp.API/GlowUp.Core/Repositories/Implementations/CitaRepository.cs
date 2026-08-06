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
        var query = Detalles().Where(cita => cita.NegocioId == negocioId && cita.EliminadoEn == null && cita.FechaCita >= desde && cita.FechaCita <= hasta);
        if (sucursalId.HasValue) query = query.Where(cita => cita.SucursalId == sucursalId.Value);
        if (empleadoId.HasValue) query = query.Where(cita => cita.EmpleadoId == empleadoId.Value);
        return await query.OrderBy(cita => cita.Inicio).ToListAsync(cancellationToken);
    }

    public Task<List<long>> ObtenerClientesConIngresosAntesDeAsync(long negocioId, DateOnly fecha, CancellationToken cancellationToken = default) =>
        _context.Citas.AsNoTracking()
            .Where(cita => cita.NegocioId == negocioId && cita.EliminadoEn == null && cita.FechaCita < fecha &&
                (cita.Estado == "confirmed" || cita.Estado == "completed"))
            .Select(cita => cita.ClienteId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public Task<Cita?> ObtenerDetalleAsync(long id, CancellationToken cancellationToken = default) =>
        Detalles().FirstOrDefaultAsync(cita => cita.Id == id && cita.EliminadoEn == null, cancellationToken);

    public Task<Cita?> ObtenerParaEditarAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Citas.Include(cita => cita.ServiciosCita).FirstOrDefaultAsync(cita => cita.Id == id && cita.EliminadoEn == null, cancellationToken);

    public Task<List<Servicio>> ObtenerServiciosAsync(long negocioId, IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default) =>
        _context.Servicios.AsNoTracking().Where(servicio => servicio.NegocioId == negocioId && servicio.EliminadoEn == null && ids.Contains(servicio.Id) && servicio.Activo).ToListAsync(cancellationToken);

    public async Task<bool> EmpleadoPuedeOfrecerServiciosAsync(long empleadoId, IReadOnlyCollection<long> servicioIds, CancellationToken cancellationToken = default)
    {
        var assigned = _context.ServiciosEmpleados.AsNoTracking().Where(item => item.EmpleadoId == empleadoId);
        if (!await assigned.AnyAsync(cancellationToken)) return true;
        var count = await assigned.CountAsync(item => servicioIds.Contains(item.ServicioId), cancellationToken);
        return count == servicioIds.Count;
    }

    public Task<Empleado?> ObtenerEmpleadoAsync(long negocioId, long sucursalId, long empleadoId, CancellationToken cancellationToken = default) =>
        _context.Empleados.AsNoTracking().Include(empleado => empleado.HorariosEmpleados)
            .Include(empleado => empleado.EmpleadosSucursales)
            .FirstOrDefaultAsync(empleado => empleado.Id == empleadoId && empleado.NegocioId == negocioId && empleado.Estado == "active" && empleado.EliminadoEn == null &&
                empleado.EmpleadosSucursales.Any(item => item.SucursalId == sucursalId && item.Estado == "active"), cancellationToken);

    public Task<List<AusenciasEmpleado>> ObtenerAusenciasActivasAsync(long empleadoId, DateTime iniciaEn, DateTime terminaEn, CancellationToken cancellationToken = default) =>
        _context.AusenciasEmpleados.AsNoTracking().Where(item => item.EmpleadoId == empleadoId && item.Estado == "scheduled" && item.IniciaEn < terminaEn && iniciaEn < item.TerminaEn).ToListAsync(cancellationToken);

    public Task<FeriadoNegocio?> ObtenerFeriadoAsync(long negocioId, long sucursalId, DateOnly fecha, CancellationToken cancellationToken = default) =>
        _context.FeriadosNegocios.AsNoTracking().FirstOrDefaultAsync(item => item.NegocioId == negocioId && item.SucursalId == sucursalId && item.Fecha == fecha, cancellationToken);

    public Task<Sucursal?> ObtenerSucursalAsync(long negocioId, long sucursalId, CancellationToken cancellationToken = default) =>
        _context.Sucursales.AsNoTracking().Include(sucursal => sucursal.HorariosNegocios)
            .FirstOrDefaultAsync(sucursal => sucursal.Id == sucursalId && sucursal.NegocioId == negocioId && sucursal.Estado == "active", cancellationToken);

    public Task<Cliente?> ObtenerClienteAsync(long clienteId, CancellationToken cancellationToken = default) =>
        _context.Clientes.AsNoTracking().FirstOrDefaultAsync(cliente => cliente.Id == clienteId, cancellationToken);

    public Task<ClientesNegocio?> ObtenerClienteNegocioAsync(long negocioId, long clienteId, CancellationToken cancellationToken = default) =>
        _context.ClientesNegocios.AsNoTracking().FirstOrDefaultAsync(item => item.NegocioId == negocioId && item.ClienteId == clienteId && item.EliminadoEn == null && item.Estado == "active", cancellationToken);

    public Task<List<Cita>> ObtenerCitasParaConflictoAsync(long empleadoId, DateTime inicio, DateTime fin, long? excluirCitaId, CancellationToken cancellationToken = default) =>
        _context.Citas.AsNoTracking().Include(cita => cita.ServiciosCita)
            .Where(cita => cita.EmpleadoId == empleadoId && cita.EliminadoEn == null && cita.Estado != "cancelled" && cita.Estado != "no_show" &&
                (!excluirCitaId.HasValue || cita.Id != excluirCitaId.Value) &&
                cita.Inicio < fin.AddDays(1) && cita.Fin > inicio.AddDays(-1))
            .ToListAsync(cancellationToken);

    public Task<List<Sucursal>> ObtenerSucursalesAsync(long negocioId, CancellationToken cancellationToken = default) =>
        _context.Sucursales.AsNoTracking().Where(item => item.NegocioId == negocioId && item.Estado == "active").OrderBy(item => item.Nombre).ToListAsync(cancellationToken);

    public Task<List<ClientesNegocio>> ObtenerClientesAsync(long negocioId, CancellationToken cancellationToken = default) =>
        _context.ClientesNegocios.AsNoTracking().Include(item => item.Cliente).Where(item => item.NegocioId == negocioId && item.EliminadoEn == null && item.Estado == "active").OrderBy(item => item.Cliente.Nombre).ToListAsync(cancellationToken);

    public Task<List<Empleado>> ObtenerEmpleadosAsync(long negocioId, long? sucursalId, CancellationToken cancellationToken = default) =>
        _context.Empleados.AsNoTracking().Include(item => item.HorariosEmpleados).Include(item => item.AusenciasEmpleados)
            .Include(item => item.EmpleadosSucursales)
            .Where(item => item.NegocioId == negocioId && item.Estado == "active" && item.EliminadoEn == null &&
                (!sucursalId.HasValue || item.EmpleadosSucursales.Any(sucursal => sucursal.SucursalId == sucursalId && sucursal.Estado == "active")))
            .OrderBy(item => item.Nombre).ToListAsync(cancellationToken);

    public Task<List<Servicio>> ObtenerServiciosActivosAsync(long negocioId, CancellationToken cancellationToken = default) =>
        _context.Servicios.AsNoTracking().Where(item => item.NegocioId == negocioId && item.EliminadoEn == null && item.Activo).OrderBy(item => item.Nombre).ToListAsync(cancellationToken);

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
        .Include(cita => cita.ServiciosCita);
}
