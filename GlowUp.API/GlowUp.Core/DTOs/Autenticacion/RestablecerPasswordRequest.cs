using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.DTOs.Autenticacion;

public sealed class RestablecerPasswordRequest
{
    [Required] public string Token { get; set; } = null!;
    [Required, MinLength(8), MaxLength(100)] public string NuevaPassword { get; set; } = null!;
}
