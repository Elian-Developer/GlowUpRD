namespace GlowUpRD.API.DTOs.Citas;

public sealed record CitaServicioResponse(long Id, long ServicioId, string Nombre, int DuracionMinutos, decimal Precio, int MinutosAntes, int MinutosDespues);

public sealed record CitaResponse(
    long Id, long NegocioId, long SucursalId, string Sucursal,
    long ClienteId, string Cliente, long EmpleadoId, string Empleado,
    DateOnly Fecha, DateTime Inicio, DateTime Fin, string Estado,
    string? MotivoCancelacion, string? Notas, decimal Total,
    IReadOnlyList<CitaServicioResponse> Servicios);
