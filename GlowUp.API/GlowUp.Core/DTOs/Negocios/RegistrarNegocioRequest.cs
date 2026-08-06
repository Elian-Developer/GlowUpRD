using System.ComponentModel.DataAnnotations;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.DTOs.Negocios;

public sealed class RegistrarNegocioRequest
{
    [Required, MaxLength(150), CommercialText] public string Nombre { get; set; } = null!;
    [Required] public string TipoNegocio { get; set; } = null!;
    [MaxLength(30)] public string? Rnc { get; set; }
    [MaxLength(30), ValidPhone] public string? Telefono { get; set; }
    [MaxLength(150), RealisticEmail] public string? Correo { get; set; }
    [MaxLength(2000)] public string? Descripcion { get; set; }

    [Required, MaxLength(255), CommercialText] public string Direccion { get; set; } = null!;
    [Required, MaxLength(100)] public string Ciudad { get; set; } = null!;
    [Required, MaxLength(100)] public string Provincia { get; set; } = null!;

    [Required, MaxLength(100), PersonName] public string NombrePropietario { get; set; } = null!;
    [Required, MaxLength(100), PersonName] public string ApellidoPropietario { get; set; } = null!;
    [Required, RealisticEmail, MaxLength(150)] public string CorreoPropietario { get; set; } = null!;
    [Required, MinLength(8), MaxLength(100)] public string Password { get; set; } = null!;
    [Required, MinLength(8), MaxLength(100)] public string ConfirmarPassword { get; set; } = null!;
    public bool RecordarSesion { get; set; }
}
