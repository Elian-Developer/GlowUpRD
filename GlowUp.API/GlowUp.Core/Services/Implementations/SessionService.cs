using System.Security.Cryptography;
using System.Text;
using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GlowUpRD.API.Services.Implementations;

public sealed class SessionService : ISessionService
{
    private readonly IRefreshTokenRepository _tokens;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public SessionService(IRefreshTokenRepository tokens, ITokenService tokenService, IConfiguration configuration)
    {
        _tokens = tokens;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<SesionAutenticada> CrearAsync(Usuario usuario, bool persistir, CancellationToken cancellationToken = default) =>
        await CrearAsync(usuario, Guid.NewGuid(), persistir, cancellationToken);

    public async Task<SesionAutenticada?> RefrescarAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;

        var token = await _tokens.ObtenerPorHashAsync(Hash(refreshToken), cancellationToken);
        var now = DateTime.UtcNow;
        if (token is null) return null;

        // Un token ya usado indica posible robo. Se invalida toda su familia, no solo ese token.
        if (token.RevocadoEn.HasValue)
        {
            await _tokens.RevocarFamiliaAsync(token.FamiliaId, now, cancellationToken);
            return null;
        }

        if (token.ExpiraEn <= now || token.Usuario.Estado != "active")
        {
            token.RevocadoEn = now;
            await _tokens.GuardarCambiosAsync(cancellationToken);
            return null;
        }

        token.RevocadoEn = now;
        var session = await CrearAsync(token.Usuario, token.FamiliaId, token.Persistente, cancellationToken);
        await _tokens.GuardarCambiosAsync(cancellationToken);
        return session;
    }

    public async Task RevocarAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var token = await _tokens.ObtenerPorHashAsync(Hash(refreshToken), cancellationToken);
        if (token is null || token.RevocadoEn.HasValue) return;
        token.RevocadoEn = DateTime.UtcNow;
        await _tokens.GuardarCambiosAsync(cancellationToken);
    }

    public Task RevocarTodasDelUsuarioAsync(long usuarioId, CancellationToken cancellationToken = default) =>
        _tokens.RevocarUsuarioAsync(usuarioId, DateTime.UtcNow, cancellationToken);

    private async Task<SesionAutenticada> CrearAsync(Usuario usuario, Guid familiaId, bool persistir, CancellationToken cancellationToken)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var expires = DateTime.UtcNow.AddDays(_configuration.GetValue("Jwt:RefreshExpirationDays", 14));
        await _tokens.AgregarAsync(new TokenActualizacion
        {
            UsuarioId = usuario.Id,
            TokenHash = Hash(rawToken),
            FamiliaId = familiaId,
            ExpiraEn = expires,
            CreadoEn = DateTime.UtcNow,
            Persistente = persistir,
        }, cancellationToken);
        await _tokens.GuardarCambiosAsync(cancellationToken);
        return new SesionAutenticada(_tokenService.CrearToken(usuario), rawToken, expires, persistir);
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
