namespace GloupUpRD.API.DTOs.Autenticacion;

public sealed record UsuarioResponse(
    ulong Id,
    string Nombre,
    string Apellido,
    string Correo,
    bool Activo,
    DateTime FechaCreacion);
