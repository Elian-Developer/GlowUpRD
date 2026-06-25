using GloupUpRD.API.Data;
using GloupUpRD.API.Models;
using GloupUpRD.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloupUpRD.API.Repositories.Implementations;

public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly GlowUpDbContext _context;

    public UsuarioRepository(GlowUpDbContext context)
    {
        _context = context;
    }

    public Task<User?> ObtenerPorIdAsync(ulong id, CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);

    public Task<User?> ObtenerPorCorreoAsync(
        string correo,
        CancellationToken cancellationToken = default) =>
        _context.Users.FirstOrDefaultAsync(usuario => usuario.Email == correo, cancellationToken);

    public async Task<List<User>> BuscarAsync(
        string? termino,
        CancellationToken cancellationToken = default)
    {
        var consulta = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(termino))
        {
            var filtro = termino.Trim();
            consulta = consulta.Where(usuario =>
                usuario.FirstName.Contains(filtro) ||
                usuario.LastName.Contains(filtro) ||
                usuario.Email.Contains(filtro));
        }

        return await consulta.OrderBy(usuario => usuario.FirstName).ToListAsync(cancellationToken);
    }

    public Task AgregarAsync(User usuario, CancellationToken cancellationToken = default) =>
        _context.Users.AddAsync(usuario, cancellationToken).AsTask();

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
