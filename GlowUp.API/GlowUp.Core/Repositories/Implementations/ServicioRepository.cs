using GlowUpRD.API.Data;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Repositories.Implementations;

public sealed class ServicioRepository : IServicioRepository
{
    private readonly GlowUpDbContext _context;
    public ServicioRepository(GlowUpDbContext context) => _context = context;

    public Task<bool> UsuarioTieneAccesoAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default) =>
        _context.Negocios.AnyAsync(negocio => negocio.Id == negocioId && negocio.Estado == "active" &&
            (negocio.UsuarioPropietarioId == usuarioId || negocio.MiembrosNegocios.Any(member => member.UsuarioId == usuarioId && member.Estado == "active")), cancellationToken);

    public Task<List<Servicio>> BuscarAsync(long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default) =>
        _context.Servicios.AsNoTracking().Include(item => item.Categoria)
            .Where(item => item.NegocioId == negocioId && item.EliminadoEn == null && (incluirInactivos || item.Activo))
            .OrderBy(item => item.Nombre).ToListAsync(cancellationToken);

    public Task<Servicio?> ObtenerAsync(long id, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = _context.Servicios.Include(item => item.Categoria).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.Id == id && item.EliminadoEn == null, cancellationToken);
    }

    public Task<bool> CategoriaValidaAsync(long negocioId, long categoriaId, CancellationToken cancellationToken = default) =>
        _context.CategoriasServicios.AnyAsync(item => item.Id == categoriaId && item.NegocioId == negocioId && item.Activo, cancellationToken);

    public Task<bool> NombreDuplicadoAsync(long negocioId, string nombre, long? excluirId, CancellationToken cancellationToken = default) =>
        _context.Servicios.AnyAsync(item => item.NegocioId == negocioId && item.EliminadoEn == null &&
            item.Nombre.Trim().ToLower() == nombre.Trim().ToLower() &&
            (!excluirId.HasValue || item.Id != excluirId.Value), cancellationToken);

    public Task<List<CategoriasServicio>> ObtenerCategoriasAsync(long negocioId, CancellationToken cancellationToken = default) =>
        _context.CategoriasServicios.AsNoTracking().Where(item => item.NegocioId == negocioId && item.Activo)
            .OrderBy(item => item.Orden).ThenBy(item => item.Nombre).ToListAsync(cancellationToken);

    public Task AgregarAsync(Servicio servicio, CancellationToken cancellationToken = default) => _context.Servicios.AddAsync(servicio, cancellationToken).AsTask();
    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);
}
