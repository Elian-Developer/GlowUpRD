using System.ComponentModel.DataAnnotations;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.DTOs.Autenticacion;

public sealed class LoginRequest
{
    [Required, RealisticEmail, MaxLength(150)]
    public string Correo { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;

    public bool RecordarSesion { get; set; }
}
