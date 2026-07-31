namespace GlowUpRD.API.Services.Interfaces;

public interface IEmailSender
{
    Task EnviarRestablecimientoPasswordAsync(string correo, string token, CancellationToken cancellationToken = default);
}
