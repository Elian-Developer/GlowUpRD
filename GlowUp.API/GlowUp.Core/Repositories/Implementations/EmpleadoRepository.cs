using GlowUpRD.API.Data;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Repositories.Implementations;

public sealed class EmpleadoRepository : IEmpleadoRepository
{
    private readonly GlowUpDbContext _context;
    public EmpleadoRepository(GlowUpDbContext context) => _context = context;

    public Task<List<Empleado>> BuscarAsync(long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default) =>
        _context.Empleados.AsNoTracking().Include(item => item.Sucursal).Include(item => item.HorariosEmpleados)
            .Where(item => item.NegocioId == negocioId && (incluirInactivos || item.Estado == "active"))
            .OrderBy(item => item.Nombre).ToListAsync(cancellationToken);

    public Task<Empleado?> ObtenerAsync(long id, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = _context.Empleados.Include(item => item.Sucursal).Include(item => item.HorariosEmpleados).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<List<Empleado>> ObtenerTodosParaHorariosAsync(CancellationToken cancellationToken = default) =>
        _context.Empleados.Include(item => item.HorariosEmpleados).ToListAsync(cancellationToken);

    public async Task<List<HorariosNegocio>> ObtenerHorariosBaseAsync(long negocioId, CancellationToken cancellationToken = default) =>
        await _context.Sucursales.AsNoTracking().Where(item => item.NegocioId == negocioId && item.EsPrincipal)
            .SelectMany(item => item.HorariosNegocios).OrderBy(item => item.DiaSemana).ToListAsync(cancellationToken);

    public Task<bool> SucursalValidaAsync(long negocioId, long sucursalId, CancellationToken cancellationToken = default) =>
        _context.Sucursales.AnyAsync(item => item.Id == sucursalId && item.NegocioId == negocioId && item.Estado == "active", cancellationToken);

    public async Task AgregarAsync(Empleado empleado, CancellationToken cancellationToken = default) =>
        await _context.Empleados.AddAsync(empleado, cancellationToken);

    public async Task AgregarConUsuarioAsync(Empleado empleado, Usuario usuario, MiembrosNegocio miembro, CancellationToken cancellationToken = default)
    {
        empleado.Usuario = usuario;
        _context.Empleados.Add(empleado);
        _context.Usuarios.Add(usuario);
        _context.MiembrosNegocios.Add(miembro);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public void ReemplazarHorarios(Empleado empleado, IEnumerable<HorariosEmpleado> horarios)
    {
        _context.HorariosEmpleados.RemoveRange(empleado.HorariosEmpleados);
        _context.HorariosEmpleados.AddRange(horarios);
    }

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);
}
