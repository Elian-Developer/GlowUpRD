using GlowUpRD.API.DTOs.Servicios;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using GlowUpRD.API.Services.Interfaces;

namespace GlowUpRD.API.Services.Implementations;

public sealed class ServicioService : IServicioService
{
    private readonly IServicioRepository _repository;
    public ServicioService(IServicioRepository repository) => _repository = repository;

    public async Task<MaintenanceResult<IReadOnlyList<ServicioResponse>>> BuscarAsync(long usuarioId, long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken)) return MaintenanceResult<IReadOnlyList<ServicioResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");
        var servicios = await _repository.BuscarAsync(negocioId, incluirInactivos, cancellationToken);
        return MaintenanceResult<IReadOnlyList<ServicioResponse>>.Ok(servicios.Select(Map).ToList());
    }

    public async Task<MaintenanceResult<ServicioResponse>> ObtenerAsync(long usuarioId, long id, CancellationToken cancellationToken = default)
    {
        var servicio = await _repository.ObtenerAsync(id, false, cancellationToken);
        if (servicio is null) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.NotFound, "El servicio no existe.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, servicio.NegocioId, cancellationToken)) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este servicio.");
        return MaintenanceResult<ServicioResponse>.Ok(Map(servicio));
    }

    public async Task<MaintenanceResult<ServicioResponse>> CrearAsync(long usuarioId, GuardarServicioRequest request, CancellationToken cancellationToken = default)
    {
        var error = await ValidateAsync(usuarioId, request, null, cancellationToken);
        if (error is not null) return error;
        var servicio = new Servicio { NegocioId = request.NegocioId, CategoriaId = request.CategoriaId, Nombre = request.Nombre.Trim(), Descripcion = Normalize(request.Descripcion), DuracionMinutos = request.DuracionMinutos, Precio = request.Precio, BufferAntesMinutos = request.MinutosAntes, BufferDespuesMinutos = request.MinutosDespues, Activo = request.Activo, CreadoEn = DateTime.UtcNow };
        await _repository.AgregarAsync(servicio, cancellationToken);
        await _repository.GuardarCambiosAsync(cancellationToken);
        return await ReloadAsync(servicio.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<ServicioResponse>> ActualizarAsync(long usuarioId, long id, GuardarServicioRequest request, CancellationToken cancellationToken = default)
    {
        var servicio = await _repository.ObtenerAsync(id, true, cancellationToken);
        if (servicio is null) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.NotFound, "El servicio no existe.");
        if (servicio.NegocioId != request.NegocioId) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.Invalid, "No se puede mover el servicio a otro negocio.");
        var error = await ValidateAsync(usuarioId, request, id, cancellationToken);
        if (error is not null) return error;

        servicio.CategoriaId = request.CategoriaId; servicio.Nombre = request.Nombre.Trim(); servicio.Descripcion = Normalize(request.Descripcion);
        servicio.DuracionMinutos = request.DuracionMinutos; servicio.Precio = request.Precio; servicio.BufferAntesMinutos = request.MinutosAntes;
        servicio.BufferDespuesMinutos = request.MinutosDespues; servicio.Activo = request.Activo; servicio.ActualizadoEn = DateTime.UtcNow;
        await _repository.GuardarCambiosAsync(cancellationToken);
        return await ReloadAsync(servicio.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<bool>> EliminarAsync(long usuarioId, long id, CancellationToken cancellationToken = default)
    {
        var servicio = await _repository.ObtenerAsync(id, true, cancellationToken);
        if (servicio is null) return MaintenanceResult<bool>.Fail(MaintenanceStatus.NotFound, "El servicio no existe.");
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, servicio.NegocioId, cancellationToken)) return MaintenanceResult<bool>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este servicio.");
        servicio.Activo = false; servicio.ActualizadoEn = DateTime.UtcNow;
        await _repository.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    public async Task<MaintenanceResult<IReadOnlyList<CategoriaServicioResponse>>> ObtenerCategoriasAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default)
    {
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken)) return MaintenanceResult<IReadOnlyList<CategoriaServicioResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");
        var categorias = await _repository.ObtenerCategoriasAsync(negocioId, cancellationToken);
        return MaintenanceResult<IReadOnlyList<CategoriaServicioResponse>>.Ok(categorias.Select(item => new CategoriaServicioResponse(item.Id, item.Nombre, item.Descripcion)).ToList());
    }

    private async Task<MaintenanceResult<ServicioResponse>?> ValidateAsync(long usuarioId, GuardarServicioRequest request, long? excludeId, CancellationToken cancellationToken)
    {
        if (!await _repository.UsuarioTieneAccesoAsync(usuarioId, request.NegocioId, cancellationToken)) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");
        if (request.CategoriaId.HasValue && !await _repository.CategoriaValidaAsync(request.NegocioId, request.CategoriaId.Value, cancellationToken)) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.Invalid, "La categoría no pertenece al negocio o está inactiva.");
        if (await _repository.NombreDuplicadoAsync(request.NegocioId, request.Nombre.Trim(), excludeId, cancellationToken)) return MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.Conflict, "Ya existe un servicio con ese nombre.");
        return null;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private async Task<MaintenanceResult<ServicioResponse>> ReloadAsync(long id, CancellationToken cancellationToken)
    {
        var servicio = await _repository.ObtenerAsync(id, false, cancellationToken);
        return servicio is null ? MaintenanceResult<ServicioResponse>.Fail(MaintenanceStatus.NotFound, "No se pudo recargar el servicio.") : MaintenanceResult<ServicioResponse>.Ok(Map(servicio));
    }
    private static ServicioResponse Map(Servicio item) => new(item.Id, item.NegocioId, item.CategoriaId, item.Categoria?.Nombre, item.Nombre, item.Descripcion, item.DuracionMinutos, item.Precio, item.BufferAntesMinutos, item.BufferDespuesMinutos, item.Activo);
}
