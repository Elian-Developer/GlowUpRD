namespace GlowUpRD.API.DTOs.Reportes;

public sealed record OcupacionDiaResponse(int DiaSemana, int Porcentaje);

public sealed record ReporteResponse(
    decimal IngresosTotales,
    int TasaConfirmacion,
    int ServiciosAgendados,
    IReadOnlyList<OcupacionDiaResponse> OcupacionSemanal);
