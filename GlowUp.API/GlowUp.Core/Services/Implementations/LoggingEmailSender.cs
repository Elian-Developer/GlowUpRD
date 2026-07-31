using GlowUpRD.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GlowUpRD.API.Services.Implementations;

/// <summary>
/// Development stand-in for a real transactional email provider: instead of sending mail,
/// it logs the reset link so the flow is testable locally. Swap the DI registration for a
/// real IEmailSender implementation before deploying.
/// </summary>
public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;
    private readonly IConfiguration _configuration;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task EnviarRestablecimientoPasswordAsync(string correo, string token, CancellationToken cancellationToken = default)
    {
        var frontendUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var enlace = $"{frontendUrl}/restablecer-password?token={token}";
        _logger.LogInformation("[DEV] Enlace de restablecimiento de contraseña para {Correo}: {Enlace}", correo, enlace);
        return Task.CompletedTask;
    }
}
