using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.DTOs.Ausencias;

public sealed class GuardarAusenciaRequest
{
    [Range(1, long.MaxValue)] public long NegocioId { get; set; }
    [Range(1, long.MaxValue)] public long EmpleadoId { get; set; }
    public DateTime IniciaEn { get; set; }
    public DateTime TerminaEn { get; set; }
    [Required, RegularExpression("^(vacation|permission|absence)$")] public string Tipo { get; set; } = "absence";
    [MaxLength(255)] public string? Motivo { get; set; }
}
