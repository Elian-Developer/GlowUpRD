using GlowUpRD.API.Data;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Repositories.Implementations;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly GlowUpDbContext _context;
    public RefreshTokenRepository(GlowUpDbContext context) => _context = context;

    public Task<TokenActualizacion?> ObtenerPorHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _context.TokensActualizacion.Include(token => token.Usuario)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public Task AgregarAsync(TokenActualizacion token, CancellationToken cancellationToken = default) =>
        _context.TokensActualizacion.AddAsync(token, cancellationToken).AsTask();

    public async Task RevocarFamiliaAsync(Guid familiaId, DateTime revocadoEn, CancellationToken cancellationToken = default) =>
        await _context.TokensActualizacion
            .Where(token => token.FamiliaId == familiaId && token.RevocadoEn == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevocadoEn, revocadoEn), cancellationToken);

    public async Task RevocarUsuarioAsync(long usuarioId, DateTime revocadoEn, CancellationToken cancellationToken = default) =>
        await _context.TokensActualizacion
            .Where(token => token.UsuarioId == usuarioId && token.RevocadoEn == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevocadoEn, revocadoEn), cancellationToken);

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken = default) =>
        await _context.SaveChangesAsync(cancellationToken);
}
