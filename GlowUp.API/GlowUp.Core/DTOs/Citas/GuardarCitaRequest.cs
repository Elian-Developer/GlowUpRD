using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.DTOs.Citas;

public sealed class GuardarCitaRequest
{
    [Range(1, long.MaxValue)] public long NegocioId { get; set; }
    [Range(1, long.MaxValue)] public long SucursalId { get; set; }
    [Range(1, long.MaxValue)] public long ClienteId { get; set; }
    [Range(1, long.MaxValue)] public long EmpleadoId { get; set; }
    public DateTime Inicio { get; set; }
    [Required, MinLength(1)] public List<long> ServicioIds { get; set; } = [];
    [MaxLength(30)] public string Estado { get; set; } = "confirmed";
    [MaxLength(255)] public string? MotivoCancelacion { get; set; }
    [MaxLength(2000)] public string? Notas { get; set; }
}
