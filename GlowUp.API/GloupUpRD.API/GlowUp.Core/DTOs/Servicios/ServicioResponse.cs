namespace GloupUpRD.API.DTOs.Servicios;

public sealed record ServicioResponse(
    long Id, long NegocioId, long? CategoriaId, string? Categoria,
    string Nombre, string? Descripcion, int DuracionMinutos, decimal Precio,
    int MinutosAntes, int MinutosDespues, bool Activo);

public sealed record CategoriaServicioResponse(long Id, string Nombre, string? Descripcion);
