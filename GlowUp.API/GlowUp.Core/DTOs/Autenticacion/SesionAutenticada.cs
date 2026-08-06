namespace GlowUpRD.API.DTOs.Autenticacion;

// El RefreshToken solo existe dentro de la API y se entrega al navegador como cookie HttpOnly.
public sealed record SesionAutenticada(LoginResponse Respuesta, string RefreshToken, DateTime RefreshExpiraEnUtc, bool Persistir);
