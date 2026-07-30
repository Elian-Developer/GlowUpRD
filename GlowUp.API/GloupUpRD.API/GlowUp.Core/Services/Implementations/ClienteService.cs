using GloupUpRD.API.DTOs.Clientes;
using GloupUpRD.API.Models;
using GloupUpRD.API.Repositories.Interfaces;
using GloupUpRD.API.Services.Interfaces;

namespace GloupUpRD.API.Services.Implementations;

public sealed class ClienteService : IClienteService
{
    private readonly IClienteRepository _clientes;
    private readonly INegocioRepository _negocios;

    public ClienteService(IClienteRepository clientes, INegocioRepository negocios)
    {
        _clientes = clientes;
        _negocios = negocios;
    }

    public async Task<MaintenanceResult<IReadOnlyList<ClienteResponse>>> BuscarAsync(long usuarioId, long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<IReadOnlyList<ClienteResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var clientes = await _clientes.BuscarAsync(negocioId, incluirInactivos, cancellationToken);
        return MaintenanceResult<IReadOnlyList<ClienteResponse>>.Ok(clientes.Select(Map).ToList());
    }

    public async Task<MaintenanceResult<ClienteResponse>> ObtenerAsync(long usuarioId, long id, CancellationToken cancellationToken = default)
    {
        var relacion = await _clientes.ObtenerAsync(id, false, cancellationToken);
        if (relacion is null) return MaintenanceResult<ClienteResponse>.Fail(MaintenanceStatus.NotFound, "El cliente no existe.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, relacion.NegocioId, cancellationToken))
            return MaintenanceResult<ClienteResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este cliente.");
        return MaintenanceResult<ClienteResponse>.Ok(Map(relacion));
    }

    public async Task<MaintenanceResult<ClienteResponse>> CrearAsync(long usuarioId, GuardarClienteRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, request.NegocioId, cancellationToken))
            return MaintenanceResult<ClienteResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var cliente = new Cliente
        {
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Telefono = Normalize(request.Telefono),
            Correo = Normalize(request.Correo),
            FechaNacimiento = request.FechaNacimiento,
            Genero = request.Genero,
            Notas = Normalize(request.Notas),
            CreadoEn = DateTime.UtcNow,
        };
        var relacion = new ClientesNegocio
        {
            NegocioId = request.NegocioId,
            Estado = "active",
            CreadoEn = DateTime.UtcNow,
        };

        await _clientes.AgregarAsync(cliente, relacion, cancellationToken);
        return await ReloadAsync(cliente.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<ClienteResponse>> ActualizarAsync(long usuarioId, long id, GuardarClienteRequest request, CancellationToken cancellationToken = default)
    {
        var relacion = await _clientes.ObtenerAsync(id, true, cancellationToken);
        if (relacion is null) return MaintenanceResult<ClienteResponse>.Fail(MaintenanceStatus.NotFound, "El cliente no existe.");
        if (relacion.NegocioId != request.NegocioId) return MaintenanceResult<ClienteResponse>.Fail(MaintenanceStatus.Invalid, "No se puede mover el cliente a otro negocio.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, request.NegocioId, cancellationToken))
            return MaintenanceResult<ClienteResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        relacion.Cliente.Nombre = request.Nombre.Trim();
        relacion.Cliente.Apellido = request.Apellido.Trim();
        relacion.Cliente.Telefono = Normalize(request.Telefono);
        relacion.Cliente.Correo = Normalize(request.Correo);
        relacion.Cliente.FechaNacimiento = request.FechaNacimiento;
        relacion.Cliente.Genero = request.Genero;
        relacion.Cliente.Notas = Normalize(request.Notas);
        relacion.Cliente.ActualizadoEn = DateTime.UtcNow;

        await _clientes.GuardarCambiosAsync(cancellationToken);
        return await ReloadAsync(id, cancellationToken);
    }

    public async Task<MaintenanceResult<bool>> EliminarAsync(long usuarioId, long id, CancellationToken cancellationToken = default)
    {
        var relacion = await _clientes.ObtenerAsync(id, true, cancellationToken);
        if (relacion is null) return MaintenanceResult<bool>.Fail(MaintenanceStatus.NotFound, "El cliente no existe.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, relacion.NegocioId, cancellationToken))
            return MaintenanceResult<bool>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este cliente.");

        relacion.Estado = "inactive";
        await _clientes.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<MaintenanceResult<ClienteResponse>> ReloadAsync(long id, CancellationToken cancellationToken)
    {
        var relacion = await _clientes.ObtenerAsync(id, false, cancellationToken);
        return relacion is null ? MaintenanceResult<ClienteResponse>.Fail(MaintenanceStatus.NotFound, "No se pudo recargar el cliente.") : MaintenanceResult<ClienteResponse>.Ok(Map(relacion));
    }

    private static ClienteResponse Map(ClientesNegocio item) => new(
        item.Cliente.Id, item.Cliente.Nombre, item.Cliente.Apellido, item.Cliente.Telefono, item.Cliente.Correo,
        item.Cliente.FechaNacimiento, item.Cliente.Genero, item.Cliente.Notas, item.Estado, item.TotalVisitas);
}
