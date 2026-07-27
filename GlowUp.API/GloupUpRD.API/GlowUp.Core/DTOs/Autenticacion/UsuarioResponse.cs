namespace GloupUpRD.API.DTOs.Autenticacion;

public sealed record UsuarioResponse(
    long Id,
    string Nombre,
    string Apellido,
    string Correo,
    bool Activo,
    DateTime FechaCreacion);
