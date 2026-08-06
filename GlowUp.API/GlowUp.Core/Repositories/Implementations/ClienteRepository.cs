using GlowUpRD.API.Data;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Repositories.Implementations;

public sealed class ClienteRepository : IClienteRepository
{
    private readonly GlowUpDbContext _context;
    public ClienteRepository(GlowUpDbContext context) => _context = context;

    public Task<List<ClientesNegocio>> BuscarAsync(long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default) =>
        _context.ClientesNegocios.AsNoTracking().Include(item => item.Cliente)
            .Where(item => item.NegocioId == negocioId && item.EliminadoEn == null && (incluirInactivos || item.Estado == "active"))
            .OrderBy(item => item.Cliente.Nombre).ToListAsync(cancellationToken);

    public Task<ClientesNegocio?> ObtenerAsync(long clienteId, bool tracking, CancellationToken cancellationToken = default)
    {
        var query = _context.ClientesNegocios.Include(item => item.Cliente).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(item => item.ClienteId == clienteId && item.EliminadoEn == null, cancellationToken);
    }

    public Task<int> ContarNuevosAsync(long negocioId, DateOnly desde, DateOnly hasta, CancellationToken cancellationToken = default)
    {
        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var finExclusivo = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);
        return _context.ClientesNegocios.AsNoTracking().CountAsync(item =>
            item.NegocioId == negocioId && item.EliminadoEn == null && item.CreadoEn >= inicio && item.CreadoEn < finExclusivo,
            cancellationToken);
    }

    public async Task AgregarAsync(Cliente cliente, ClientesNegocio relacion, CancellationToken cancellationToken = default)
    {
        relacion.Cliente = cliente;
        _context.Clientes.Add(cliente);
        _context.ClientesNegocios.Add(relacion);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => await _context.SaveChangesAsync(cancellationToken);
}
