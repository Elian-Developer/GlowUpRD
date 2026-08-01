namespace GlowUpRD.API.DTOs.Reportes;

public sealed record OcupacionDiaResponse(int DiaSemana, int Porcentaje);
public sealed record ReporteDiaResponse(DateOnly Fecha, int Citas, decimal Ingresos);
public sealed record ReporteEmpleadoResponse(long EmpleadoId, string Nombre, int Citas, decimal Ingresos, int OcupacionAgenda);
public sealed record ReporteServicioResponse(string Nombre, int Cantidad, decimal Ingresos);
public sealed record ReporteEstadoResponse(string Estado, int Cantidad);

public sealed record ReporteResponse(
    decimal IngresosTotales,
    int TasaConfirmacion,
    int ServiciosAgendados,
    IReadOnlyList<OcupacionDiaResponse> OcupacionSemanal,
    int CitasTotales,
    int CitasCompletadas,
    int CitasPendientes,
    int CitasCanceladas,
    int ClientesUnicos,
    decimal TicketPromedio,
    int ClientesNuevos,
    int TasaRetencion,
    int OcupacionAgenda,
    int TasaCancelacion,
    int TasaNoConfirmadas,
    IReadOnlyList<ReporteDiaResponse> EvolucionDiaria,
    IReadOnlyList<ReporteEmpleadoResponse> RendimientoEmpleados,
    IReadOnlyList<ReporteServicioResponse> RendimientoServicios,
    IReadOnlyList<ReporteEstadoResponse> EstadosCitas);
