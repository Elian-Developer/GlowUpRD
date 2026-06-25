namespace GloupUpRD.API.DTOs.Servicios;

public sealed record ServicioResponse(
    ulong Id, ulong NegocioId, ulong? CategoriaId, string? Categoria,
    string Nombre, string? Descripcion, uint DuracionMinutos, decimal Precio,
    uint MinutosAntes, uint MinutosDespues, bool Activo);

public sealed record CategoriaServicioResponse(ulong Id, string Nombre, string? Descripcion);
