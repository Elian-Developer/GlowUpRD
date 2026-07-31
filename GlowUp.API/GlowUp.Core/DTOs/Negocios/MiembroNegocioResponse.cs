namespace GlowUpRD.API.DTOs.Negocios;

public sealed record MiembroNegocioResponse(
    long UsuarioId,
    string Nombre,
    string Apellido,
    string Correo,
    string Rol,
    string Estado);
