namespace GloupUpRD.API.DTOs.Citas;

public sealed record CitaServicioResponse(ulong Id, ulong ServicioId, string Nombre, uint DuracionMinutos, decimal Precio);

public sealed record CitaResponse(
    ulong Id, ulong NegocioId, ulong SucursalId, string Sucursal,
    ulong ClienteId, string Cliente, ulong EmpleadoId, string Empleado,
    DateOnly Fecha, DateTime Inicio, DateTime Fin, string Estado,
    string? MotivoCancelacion, string? Notas, decimal Total,
    IReadOnlyList<CitaServicioResponse> Servicios);
