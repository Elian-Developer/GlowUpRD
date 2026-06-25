using System.ComponentModel.DataAnnotations;

namespace GloupUpRD.API.DTOs.Autenticacion;

public sealed class RegistrarUsuarioRequest
{
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Apellido { get; set; } = null!;

    [Required, EmailAddress, MaxLength(255)]
    public string Correo { get; set; } = null!;

    [Required, MinLength(8), MaxLength(100)]
    public string Password { get; set; } = null!;
}
