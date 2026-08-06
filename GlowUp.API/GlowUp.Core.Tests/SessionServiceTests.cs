using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using GlowUpRD.API.Services.Implementations;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GlowUpRD.API.Core.Tests;

public sealed class SessionServiceTests
{
    [Fact]
    public async Task CreateSession_StoresOnlyHashOfRefreshToken()
    {
        var tokens = new InMemoryRefreshTokenRepository();
        var service = CreateService(tokens);

        var session = await service.CrearAsync(ActiveUser(), persistir: true);

        var stored = Assert.Single(tokens.Tokens);
        Assert.NotEqual(session.RefreshToken, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);
        Assert.Equal(session.Respuesta.Usuario.Id, stored.UsuarioId);
        Assert.True(session.RefreshExpiraEnUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Refresh_RotatesTokenAndRevokesTheOldOne()
    {
        var tokens = new InMemoryRefreshTokenRepository();
        var service = CreateService(tokens);
        var first = await service.CrearAsync(ActiveUser(), persistir: true);

        var rotated = await service.RefrescarAsync(first.RefreshToken);

        Assert.NotNull(rotated);
        Assert.NotEqual(first.RefreshToken, rotated!.RefreshToken);
        Assert.Equal(2, tokens.Tokens.Count);
        Assert.NotNull(tokens.Tokens.Single(token => token.TokenHash != tokens.Tokens.Last().TokenHash).RevocadoEn);
        Assert.All(tokens.Tokens, token => Assert.Equal(tokens.Tokens[0].FamiliaId, token.FamiliaId));
    }

    [Fact]
    public async Task ReusingRotatedToken_RevokesTheEntireFamily()
    {
        var tokens = new InMemoryRefreshTokenRepository();
        var service = CreateService(tokens);
        var first = await service.CrearAsync(ActiveUser(), persistir: true);
        var rotated = await service.RefrescarAsync(first.RefreshToken);

        var reused = await service.RefrescarAsync(first.RefreshToken);

        Assert.NotNull(rotated);
        Assert.Null(reused);
        Assert.All(tokens.Tokens, token => Assert.NotNull(token.RevocadoEn));
    }

    private static SessionService CreateService(InMemoryRefreshTokenRepository tokens)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "tests-session-key-with-at-least-thirty-two-characters",
            ["Jwt:Issuer"] = "GlowUpRD.Tests",
            ["Jwt:Audience"] = "GlowUpRD.Tests",
            ["Jwt:ExpirationMinutes"] = "15",
            ["Jwt:RefreshExpirationDays"] = "14",
        }).Build();
        return new SessionService(tokens, new TokenService(configuration), configuration);
    }

    private static Usuario ActiveUser() => new()
    {
        Id = 7,
        Nombre = "María",
        Apellido = "Pérez",
        Correo = "maria@ejemplo.do",
        Estado = "active",
        ContrasenaHash = "not-used-by-session-tests",
        CreadoEn = DateTime.UtcNow,
    };

    private sealed class InMemoryRefreshTokenRepository : IRefreshTokenRepository
    {
        public List<TokenActualizacion> Tokens { get; } = [];

        public Task<TokenActualizacion?> ObtenerPorHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult<TokenActualizacion?>(Tokens.SingleOrDefault(token => token.TokenHash == tokenHash));

        public Task AgregarAsync(TokenActualizacion token, CancellationToken cancellationToken = default)
        {
            token.Usuario = ActiveUser();
            Tokens.Add(token);
            return Task.CompletedTask;
        }

        public Task RevocarFamiliaAsync(Guid familiaId, DateTime revocadoEn, CancellationToken cancellationToken = default)
        {
            foreach (var token in Tokens.Where(token => token.FamiliaId == familiaId && token.RevocadoEn is null)) token.RevocadoEn = revocadoEn;
            return Task.CompletedTask;
        }

        public Task RevocarUsuarioAsync(long usuarioId, DateTime revocadoEn, CancellationToken cancellationToken = default)
        {
            foreach (var token in Tokens.Where(token => token.UsuarioId == usuarioId && token.RevocadoEn is null)) token.RevocadoEn = revocadoEn;
            return Task.CompletedTask;
        }

        public Task GuardarCambiosAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
