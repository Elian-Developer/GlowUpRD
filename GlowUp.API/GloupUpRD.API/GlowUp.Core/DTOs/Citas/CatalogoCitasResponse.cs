namespace GloupUpRD.API.DTOs.Citas;

public sealed record CatalogoItemResponse(ulong Id, string Nombre, string? Detalle = null);
public sealed record CatalogoServicioResponse(ulong Id, string Nombre, uint DuracionMinutos, decimal Precio);
public sealed record NegocioResumenResponse(ulong Id, string Nombre, string Tipo);
public sealed record CatalogoCitasResponse(
    IReadOnlyList<CatalogoItemResponse> Sucursales,
    IReadOnlyList<CatalogoItemResponse> Clientes,
    IReadOnlyList<CatalogoItemResponse> Empleados,
    IReadOnlyList<CatalogoServicioResponse> Servicios);
