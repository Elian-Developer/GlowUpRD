namespace GloupUpRD.API.DTOs.Negocios;

public sealed record NegocioDetalleResponse(
    long Id,
    string Nombre,
    string Slug,
    string TipoNegocio,
    string? Descripcion,
    string? Rnc,
    string? Telefono,
    string? Correo,
    string? LogoUrl,
    string Estado);
