namespace GlowUpRD.API.DTOs.Negocios;

public sealed record SucursalPrincipalResponse(
    long Id, string Nombre, string? Telefono, string Direccion,
    string Ciudad, string Provincia, string Pais);

public sealed record HorarioNegocioResponse(
    short DiaSemana, TimeOnly? AbreA, TimeOnly? CierraA, bool Cerrado);

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
    string Estado,
    SucursalPrincipalResponse SucursalPrincipal,
    IReadOnlyList<HorarioNegocioResponse> Horarios);
