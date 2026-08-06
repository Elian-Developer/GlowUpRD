namespace GlowUpRD.API.DTOs.Ausencias;

public sealed record AusenciaResponse(
    long Id, long NegocioId, long EmpleadoId, string Empleado,
    DateTime IniciaEn, DateTime TerminaEn, string Tipo, string? Motivo,
    string Estado, DateTime CreadoEn);
