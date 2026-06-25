namespace GloupUpRD.API.DTOs.Autenticacion;

public sealed record LoginResponse(
    string Token,
    DateTime ExpiraEnUtc,
    UsuarioResponse Usuario);
