using System.ComponentModel.DataAnnotations;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.DTOs.Autenticacion;

public sealed class ActualizarUsuarioRequest
{
    [Required, MaxLength(100), PersonName]
    public string Nombre { get; set; } = null!;

    [Required, MaxLength(100), PersonName]
    public string Apellido { get; set; } = null!;

    [Required, RealisticEmail, MaxLength(150)]
    public string Correo { get; set; } = null!;
}
