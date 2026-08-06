using System.ComponentModel.DataAnnotations;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.DTOs.Negocios;

public sealed class CrearUsuarioNegocioRequest
{
    [Required, MaxLength(100), PersonName] public string Nombre { get; set; } = null!;
    [Required, MaxLength(100), PersonName] public string Apellido { get; set; } = null!;
    [Required, RealisticEmail, MaxLength(150)] public string Correo { get; set; } = null!;
    [Required, MinLength(8), MaxLength(100)] public string Password { get; set; } = null!;
    [Required] public string Rol { get; set; } = null!;
}
