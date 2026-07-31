using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.DTOs.Negocios;

public sealed class ActualizarNegocioRequest
{
    [Required, MaxLength(150)] public string Nombre { get; set; } = null!;
    [MaxLength(30)] public string? Telefono { get; set; }
    [EmailAddress, MaxLength(150)] public string? Correo { get; set; }
    [MaxLength(2000)] public string? Descripcion { get; set; }
    [MaxLength(500)] public string? LogoUrl { get; set; }
}
