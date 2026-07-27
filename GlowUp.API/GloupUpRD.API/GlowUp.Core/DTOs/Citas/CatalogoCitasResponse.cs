namespace GloupUpRD.API.DTOs.Citas;

public sealed record CatalogoItemResponse(long Id, string Nombre, string? Detalle = null);
public sealed record CatalogoServicioResponse(long Id, string Nombre, int DuracionMinutos, decimal Precio);
public sealed record NegocioResumenResponse(long Id, string Nombre, string Tipo);
public sealed record CatalogoCitasResponse(
    IReadOnlyList<CatalogoItemResponse> Sucursales,
    IReadOnlyList<CatalogoItemResponse> Clientes,
    IReadOnlyList<CatalogoItemResponse> Empleados,
    IReadOnlyList<CatalogoServicioResponse> Servicios);
