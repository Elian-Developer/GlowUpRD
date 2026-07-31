using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.DTOs.Clientes;

public sealed class GuardarClienteRequest
{
    [Range(1, long.MaxValue)] public long NegocioId { get; set; }
    [Required, MaxLength(100)] public string Nombre { get; set; } = null!;
    [Required, MaxLength(100)] public string Apellido { get; set; } = null!;
    [MaxLength(30)] public string? Telefono { get; set; }
    [EmailAddress, MaxLength(150)] public string? Correo { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    public string? Genero { get; set; }
    [MaxLength(2000)] public string? Notas { get; set; }
}
