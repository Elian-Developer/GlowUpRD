using GlowUpRD.API.Data;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Repositories.Implementations;

public sealed class AusenciaRepository : IAusenciaRepository
{
    private readonly GlowUpDbContext _context;
    public AusenciaRepository(GlowUpDbContext context) => _context = context;

    public async Task<List<AusenciasEmpleado>> BuscarAsync(long negocioId, DateTime desde, DateTime hasta, long? empleadoId, bool incluirCanceladas, CancellationToken cancellationToken = default)
    {
        var query = _context.AusenciasEmpleados.AsNoTracking().Include(item => item.Empleado)
            .Where(item => item.Empleado.NegocioId == negocioId && item.IniciaEn < hasta && item.TerminaEn > desde);
        if (empleadoId.HasValue) query = query.Where(item => item.EmpleadoId == empleadoId.Value);
        if (!incluirCanceladas) query = query.Where(item => item.Estado == "scheduled");
        return await query.OrderBy(item => item.IniciaEn).ToListAsync(cancellationToken);
    }

    public Task<AusenciasEmpleado?> ObtenerAsync(long id, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = _context.AusenciasEmpleados.Include(item => item.Empleado).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<Empleado?> ObtenerEmpleadoAsync(long negocioId, long empleadoId, CancellationToken cancellationToken = default) =>
        _context.Empleados.AsNoTracking().FirstOrDefaultAsync(item => item.Id == empleadoId && item.NegocioId == negocioId && item.Estado == "active" && item.EliminadoEn == null, cancellationToken);

    public Task<bool> ExisteSolapamientoAsync(long empleadoId, DateTime iniciaEn, DateTime terminaEn, long? excluirId, CancellationToken cancellationToken = default) =>
        _context.AusenciasEmpleados.AnyAsync(item => item.EmpleadoId == empleadoId && item.Estado == "scheduled" &&
            (!excluirId.HasValue || item.Id != excluirId.Value) && item.IniciaEn < terminaEn && iniciaEn < item.TerminaEn, cancellationToken);

    public Task<List<Cita>> ObtenerCitasBloqueantesAsync(long empleadoId, DateTime iniciaEn, DateTime terminaEn, CancellationToken cancellationToken = default) =>
        _context.Citas.AsNoTracking().Include(item => item.ServiciosCita)
            .Where(item => item.EmpleadoId == empleadoId && item.EliminadoEn == null && (item.Estado == "pending" || item.Estado == "confirmed" || item.Estado == "completed") &&
                item.Inicio < terminaEn.AddDays(1) && item.Fin > iniciaEn.AddDays(-1))
            .ToListAsync(cancellationToken);

    public Task AgregarAsync(AusenciasEmpleado ausencia, CancellationToken cancellationToken = default) => _context.AusenciasEmpleados.AddAsync(ausencia, cancellationToken).AsTask();
    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);
}
