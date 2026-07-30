namespace GloupUpRD.API.DTOs.Clientes;

public sealed record ClienteResponse(
    long Id,
    string Nombre,
    string Apellido,
    string? Telefono,
    string? Correo,
    DateOnly? FechaNacimiento,
    string? Genero,
    string? Notas,
    string Estado,
    int TotalVisitas);
