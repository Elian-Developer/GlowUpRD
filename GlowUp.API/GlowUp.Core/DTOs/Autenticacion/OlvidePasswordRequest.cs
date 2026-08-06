using System.ComponentModel.DataAnnotations;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.DTOs.Autenticacion;

public sealed class OlvidePasswordRequest
{
    [Required, RealisticEmail, MaxLength(150)] public string Correo { get; set; } = null!;
}
