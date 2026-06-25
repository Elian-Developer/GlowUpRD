using System.ComponentModel.DataAnnotations;

namespace GloupUpRD.API.DTOs.Citas;

public sealed class GuardarCitaRequest
{
    [Range(1, ulong.MaxValue)] public ulong NegocioId { get; set; }
    [Range(1, ulong.MaxValue)] public ulong SucursalId { get; set; }
    [Range(1, ulong.MaxValue)] public ulong ClienteId { get; set; }
    [Range(1, ulong.MaxValue)] public ulong EmpleadoId { get; set; }
    public DateTime Inicio { get; set; }
    [Required, MinLength(1)] public List<ulong> ServicioIds { get; set; } = [];
    [MaxLength(30)] public string Estado { get; set; } = "confirmed";
    [MaxLength(255)] public string? MotivoCancelacion { get; set; }
    [MaxLength(2000)] public string? Notas { get; set; }
}
