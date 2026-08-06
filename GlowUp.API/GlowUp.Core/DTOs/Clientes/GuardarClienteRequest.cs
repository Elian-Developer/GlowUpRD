using System.ComponentModel.DataAnnotations;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.DTOs.Clientes;

public sealed class GuardarClienteRequest
{
    [Range(1, long.MaxValue)] public long NegocioId { get; set; }
    [Required, MaxLength(100), PersonName] public string Nombre { get; set; } = null!;
    [Required, MaxLength(100), PersonName] public string Apellido { get; set; } = null!;
    [MaxLength(30), ValidPhone] public string? Telefono { get; set; }
    [MaxLength(150), RealisticEmail] public string? Correo { get; set; }
    public DateOnly? FechaNacimiento { get; set; }
    [RegularExpression("^(female|male|other|not_specified)$")] public string? Genero { get; set; }
    [MaxLength(2000)] public string? Notas { get; set; }
}
