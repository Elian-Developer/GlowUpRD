using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.DTOs.Autenticacion;

public sealed class OlvidePasswordRequest
{
    [Required, EmailAddress, MaxLength(255)] public string Correo { get; set; } = null!;
}
