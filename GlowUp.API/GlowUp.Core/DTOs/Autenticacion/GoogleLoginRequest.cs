using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.DTOs.Autenticacion;

public sealed class GoogleLoginRequest
{
    [Required] public string CredentialToken { get; set; } = null!;
    public bool RecordarSesion { get; set; }
}
