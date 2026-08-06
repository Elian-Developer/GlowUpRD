using GlowUpRD.API.DTOs.Clientes;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using GlowUpRD.API.Services.Interfaces;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.Services.Implementations;

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
        if (!InputRules.IsValidBirthDate(request.FechaNacimiento, DateOnly.FromDateTime(DateTime.Today)))
            return MaintenanceResult<ClienteResponse>.Fail(MaintenanceStatus.Invalid, "fechaNacimiento", "INVALID_BIRTH_DATE", "La fecha de nacimiento no puede ser posterior a hoy ni anterior a 120 años.");

        var cliente = new Cliente
        {
            Nombre = InputNormalizer.RequiredText(request.Nombre),
            Apellido = InputNormalizer.RequiredText(request.Apellido),
            Telefono = InputNormalizer.NormalizePhone(request.Telefono),
            Correo = string.IsNullOrWhiteSpace(request.Correo) ? null : InputNormalizer.NormalizeEmail(request.Correo),
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
        if (!InputRules.IsValidBirthDate(request.FechaNacimiento, DateOnly.FromDateTime(DateTime.Today)))
            return MaintenanceResult<ClienteResponse>.Fail(MaintenanceStatus.Invalid, "fechaNacimiento", "INVALID_BIRTH_DATE", "La fecha de nacimiento no puede ser posterior a hoy ni anterior a 120 años.");

        relacion.Cliente.Nombre = InputNormalizer.RequiredText(request.Nombre);
        relacion.Cliente.Apellido = InputNormalizer.RequiredText(request.Apellido);
        relacion.Cliente.Telefono = InputNormalizer.NormalizePhone(request.Telefono);
        relacion.Cliente.Correo = string.IsNullOrWhiteSpace(request.Correo) ? null : InputNormalizer.NormalizeEmail(request.Correo);
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
        relacion.EliminadoEn = DateTime.UtcNow;
        await _clientes.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    private static string? Normalize(string? value) => InputNormalizer.OptionalText(value);

    private async Task<MaintenanceResult<ClienteResponse>> ReloadAsync(long id, CancellationToken cancellationToken)
    {
        var relacion = await _clientes.ObtenerAsync(id, false, cancellationToken);
        return relacion is null ? MaintenanceResult<ClienteResponse>.Fail(MaintenanceStatus.NotFound, "No se pudo recargar el cliente.") : MaintenanceResult<ClienteResponse>.Ok(Map(relacion));
    }

    private static ClienteResponse Map(ClientesNegocio item) => new(
        item.Cliente.Id, item.Cliente.Nombre, item.Cliente.Apellido, item.Cliente.Telefono, item.Cliente.Correo,
        item.Cliente.FechaNacimiento, item.Cliente.Genero, item.Cliente.Notas, item.Estado, item.TotalVisitas);
}
