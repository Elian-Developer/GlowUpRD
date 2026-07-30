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

    public Task<Usuario?> ObtenerPorIdAsync(long id, CancellationToken cancellationToken = default) =>
        _context.Usuarios.FirstOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);

    public Task<Usuario?> ObtenerPorCorreoAsync(
        string correo,
        CancellationToken cancellationToken = default) =>
        _context.Usuarios.FirstOrDefaultAsync(usuario => usuario.Correo == correo, cancellationToken);

    public Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
        _context.Usuarios.AddAsync(usuario, cancellationToken).AsTask();

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
