using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.DTOs.Autenticacion;

public sealed class LoginRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Correo { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
