using GloupUpRD.API.DTOs.Servicios;
using GloupUpRD.API.Models;
using GloupUpRD.API.Repositories.Interfaces;
using GloupUpRD.API.Services.Interfaces;

namespace GloupUpRD.API.Services.Implementations;

public sealed class ServicioService : IServicioService
{
    private readonly IServicioRepository _repository;
    public ServicioService(IServicioRepository repository) => _repository = repository;

    public async Task<MaintenanceResult<IReadOnlyList<ServicioResponse>>> BuscarAsync(ulong usuarioId, ulong negocioId, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken)) return MaintenanceResult<IReadOnlyList<ServicioResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");
        var services = await _repository.BuscarAsync(negocioId, incluirInactivos, cancellationToken);
        return MaintenanceResult<IReadOnlyList<ServicioResponse>>.Ok(services.Select(Map).ToList());
    }

    public async Task<MaintenanceResult<ServicioResponse>> ObtenerAsync(ulong usuarioId, ulong id, CancellationToken cancellationToken = default)
    {
        var service = await _repository.ObtenerAsync(id, false, cancellationToken);
        if (service is null) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.NotFound, "El servicio no existe.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, service.BusinessId, cancellationToken)) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este servicio.");
        return MaintenanceResult<ServicioResponse>.Ok(Map(service));
    }

    public async Task<MaintenanceResult<ServicioResponse>> CrearAsync(ulong usuarioId, GuardarServicioRequest request, CancellationToken cancellationToken = default)
    {
        var error = await ValidateAsync(usuarioId, request, null, cancellationToken);
        if (error is not null) return error;
        var service = new Service { BusinessId = request.NegocioId, CategoryId = request.CategoriaId, Name = request.Nombre.Trim(), Description = Normalize(request.Descripcion), DurationMinutes = request.DuracionMinutos, Price = request.Precio, BufferBeforeMinutes = request.MinutosAntes, BufferAfterMinutes = request.MinutosDespues, IsActive = request.Activo, CreatedAt = DateTime.UtcNow };
        await _repository.AgregarAsync(service, cancellationToken);
        await _repository.GuardarCambiosAsync(cancellationToken);
        return await ReloadAsync(service.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<ServicioResponse>> ActualizarAsync(ulong usuarioId, ulong id, GuardarServicioRequest request, CancellationToken cancellationToken = default)
    {
        var service = await _repository.ObtenerAsync(id, true, cancellationToken);
        if (service is null) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.NotFound, "El servicio no existe.");
        if (service.BusinessId != request.NegocioId) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.Invalid, "No se puede mover el servicio a otro negocio.");
        var error = await ValidateAsync(usuarioId, request, id, cancellationToken);
        if (error is not null) return error;

        service.CategoryId = request.CategoriaId; service.Name = request.Nombre.Trim(); service.Description = Normalize(request.Descripcion);
        service.DurationMinutes = request.DuracionMinutos; service.Price = request.Precio; service.BufferBeforeMinutes = request.MinutosAntes;
        service.BufferAfterMinutes = request.MinutosDespues; service.IsActive = request.Activo; service.UpdatedAt = DateTime.UtcNow;
        await _repository.GuardarCambiosAsync(cancellationToken);
        return await ReloadAsync(service.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<bool>> EliminarAsync(ulong usuarioId, ulong id, CancellationToken cancellationToken = default)
    {
        var service = await _repository.ObtenerAsync(id, true, cancellationToken);
        if (service is null) return MaintenanceResult<bool>.Fail(MaintenanceStatus.NotFound, "El servicio no existe.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, service.BusinessId, cancellationToken)) return MaintenanceResult<bool>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este servicio.");
        service.IsActive = false; service.UpdatedAt = DateTime.UtcNow;
        await _repository.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    public async Task<MaintenanceResult<IReadOnlyList<CategoriaServicioResponse>>> ObtenerCategoriasAsync(ulong usuarioId, ulong negocioId, CancellationToken cancellationToken = default)
    {
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken)) return MaintenanceResult<IReadOnlyList<CategoriaServicioResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");
        var categories = await _repository.ObtenerCategoriasAsync(negocioId, cancellationToken);
        return MaintenanceResult<IReadOnlyList<CategoriaServicioResponse>>.Ok(categories.Select(item => new CategoriaServicioResponse(item.Id, item.Name, item.Description)).ToList());
    }

    private async Task<MaintenanceResult<ServicioResponse>?> ValidateAsync(ulong usuarioId, GuardarServicioRequest request, ulong? excludeId, CancellationToken cancellationToken)
    {
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, request.NegocioId, cancellationToken)) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");
        if (request.CategoriaId.HasValue && !await _repository.CategoriaValidaAsync(request.NegocioId, request.CategoriaId.Value, cancellationToken)) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.Invalid, "La categoría no pertenece al negocio o está inactiva.");
        if (await _repository.NombreDuplicadoAsync(request.NegocioId, request.Nombre.Trim(), excludeId, cancellationToken)) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.Conflict, "Ya existe un servicio con ese nombre.");
        return null;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private async Task<MaintenanceResult<ServicioResponse>> ReloadAsync(ulong id, CancellationToken cancellationToken)
    {
        var service = await _repository.ObtenerAsync(id, false, cancellationToken);
        return service is null ? MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.NotFound, "No se pudo recargar el servicio.") : MaintenanceResult<ServicioResponse>.Ok(Map(service));
    }
    private static ServicioResponse Map(Service item) => new(item.Id, item.BusinessId, item.CategoryId, item.Category?.Name, item.Name, item.Description, item.DurationMinutes, item.Price, item.BufferBeforeMinutes, item.BufferAfterMinutes, item.IsActive != false);
}
