using System.ComponentModel.DataAnnotations;

namespace GloupUpRD.API.DTOs.Autenticacion;

public sealed class GoogleLoginRequest
{
    [Required] public string CredentialToken { get; set; } = null!;
}
