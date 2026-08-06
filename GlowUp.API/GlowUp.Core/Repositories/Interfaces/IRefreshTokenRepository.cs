using GlowUpRD.API.Models;

namespace GlowUpRD.API.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task<TokenActualizacion?> ObtenerPorHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task AgregarAsync(TokenActualizacion token, CancellationToken cancellationToken = default);
    Task RevocarFamiliaAsync(Guid familiaId, DateTime revocadoEn, CancellationToken cancellationToken = default);
    Task RevocarUsuarioAsync(long usuarioId, DateTime revocadoEn, CancellationToken cancellationToken = default);
    Task GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
