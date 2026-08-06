using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.Models;

namespace GlowUpRD.API.Services.Interfaces;

public interface ISessionService
{
    Task<SesionAutenticada> CrearAsync(Usuario usuario, bool persistir, CancellationToken cancellationToken = default);
    Task<SesionAutenticada?> RefrescarAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevocarAsync(string? refreshToken, CancellationToken cancellationToken = default);
    Task RevocarTodasDelUsuarioAsync(long usuarioId, CancellationToken cancellationToken = default);
}
