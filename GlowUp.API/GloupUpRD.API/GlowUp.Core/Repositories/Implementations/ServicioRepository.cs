using GloupUpRD.API.Data;
using GloupUpRD.API.Models;
using GloupUpRD.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloupUpRD.API.Repositories.Implementations;

public sealed class ServicioRepository : IServicioRepository
{
    private readonly GlowUpDbContext _context;
    public ServicioRepository(GlowUpDbContext context) => _context = context;

    public Task<bool> UsuarioTieneAccesoAsync(ulong usuarioId, ulong negocioId, CancellationToken cancellationToken = default) =>
        _context.Businesses.AnyAsync(business => business.Id == negocioId && business.Status == "active" &&
            (business.OwnerUserId == usuarioId || business.BusinessMembers.Any(member => member.UserId == usuarioId && member.Status == "active")), cancellationToken);

    public Task<List<Service>> BuscarAsync(ulong negocioId, bool incluirInactivos, CancellationToken cancellationToken = default) =>
        _context.Services.AsNoTracking().Include(item => item.Category)
            .Where(item => item.BusinessId == negocioId && (incluirInactivos || item.IsActive != false))
            .OrderBy(item => item.Name).ToListAsync(cancellationToken);

    public Task<Service?> ObtenerAsync(ulong id, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = _context.Services.Include(item => item.Category).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<bool> CategoriaValidaAsync(ulong negocioId, ulong categoriaId, CancellationToken cancellationToken = default) =>
        _context.ServiceCategories.AnyAsync(item => item.Id == categoriaId && item.BusinessId == negocioId && item.IsActive != false, cancellationToken);

    public Task<bool> NombreDuplicadoAsync(ulong negocioId, string nombre, ulong? excluirId, CancellationToken cancellationToken = default) =>
        _context.Services.AnyAsync(item => item.BusinessId == negocioId && item.Name == nombre && (!excluirId.HasValue || item.Id != excluirId.Value), cancellationToken);

    public Task<List<ServiceCategory>> ObtenerCategoriasAsync(ulong negocioId, CancellationToken cancellationToken = default) =>
        _context.ServiceCategories.AsNoTracking().Where(item => item.BusinessId == negocioId && item.IsActive != false)
            .OrderBy(item => item.DisplayOrder).ThenBy(item => item.Name).ToListAsync(cancellationToken);

    public Task AgregarAsync(Service servicio, CancellationToken cancellationToken = default) => _context.Services.AddAsync(servicio, cancellationToken).AsTask();
    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);
}
