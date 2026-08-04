namespace GlowUpRD.API.DTOs.Empleados;

public sealed record HorarioEmpleadoResponse(short DiaSemana, TimeOnly? IniciaA, TimeOnly? TerminaA, bool Activo);

public sealed record EmpleadoResponse(
    long Id,
    long NegocioId,
    long? SucursalId,
    string? Sucursal,
    string Nombre,
    string Apellido,
    string? Telefono,
    string? Correo,
    string? Puesto,
    string? Biografia,
    string? FotoUrl,
    string Estado,
    bool TieneAcceso,
    IReadOnlyList<HorarioEmpleadoResponse> Horarios);
