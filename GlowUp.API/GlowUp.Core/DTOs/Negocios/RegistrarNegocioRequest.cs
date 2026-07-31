using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.DTOs.Negocios;

public sealed class RegistrarNegocioRequest
{
    [Required, MaxLength(150)] public string Nombre { get; set; } = null!;
    [Required] public string TipoNegocio { get; set; } = null!;
    [MaxLength(30)] public string? Rnc { get; set; }
    [MaxLength(30)] public string? Telefono { get; set; }
    [EmailAddress, MaxLength(150)] public string? Correo { get; set; }
    [MaxLength(2000)] public string? Descripcion { get; set; }

    [Required, MaxLength(255)] public string Direccion { get; set; } = null!;
    [Required, MaxLength(100)] public string Ciudad { get; set; } = null!;
    [Required, MaxLength(100)] public string Provincia { get; set; } = null!;

    [Required, MaxLength(100)] public string NombrePropietario { get; set; } = null!;
    [Required, MaxLength(100)] public string ApellidoPropietario { get; set; } = null!;
    [Required, EmailAddress, MaxLength(255)] public string CorreoPropietario { get; set; } = null!;
    [Required, MinLength(8), MaxLength(100)] public string Password { get; set; } = null!;
}
