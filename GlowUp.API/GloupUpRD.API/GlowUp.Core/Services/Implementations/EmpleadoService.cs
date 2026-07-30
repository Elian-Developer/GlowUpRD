using GloupUpRD.API.DTOs.Empleados;
using GloupUpRD.API.Models;
using GloupUpRD.API.Repositories.Interfaces;
using GloupUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace GloupUpRD.API.Services.Implementations;

public sealed class EmpleadoService : IEmpleadoService
{
    private readonly IEmpleadoRepository _empleados;
    private readonly INegocioRepository _negocios;
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public EmpleadoService(IEmpleadoRepository empleados, INegocioRepository negocios, IUsuarioRepository usuarios, IPasswordHasher<Usuario> passwordHasher)
    {
        _empleados = empleados;
        _negocios = negocios;
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
    }

    public async Task<MaintenanceResult<IReadOnlyList<EmpleadoResponse>>> BuscarAsync(long usuarioId, long negocioId, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<IReadOnlyList<EmpleadoResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var empleados = await _empleados.BuscarAsync(negocioId, incluirInactivos, cancellationToken);
        return MaintenanceResult<IReadOnlyList<EmpleadoResponse>>.Ok(empleados.Select(Map).ToList());
    }

    public async Task<MaintenanceResult<EmpleadoResponse>> ObtenerAsync(long usuarioId, long id, CancellationToken cancellationToken = default)
    {
        var empleado = await _empleados.ObtenerAsync(id, false, cancellationToken);
        if (empleado is null) return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.NotFound, "El empleado no existe.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, empleado.NegocioId, cancellationToken))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este empleado.");
        return MaintenanceResult<EmpleadoResponse>.Ok(Map(empleado));
    }

    public async Task<MaintenanceResult<EmpleadoResponse>> CrearAsync(long usuarioId, GuardarEmpleadoRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CrearAcceso)
        {
            if (!await _negocios.UsuarioEsPropietarioAsync(usuarioId, request.NegocioId, cancellationToken))
                return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Forbidden, "Solo el dueño del negocio puede crear accesos al panel.");
            if (string.IsNullOrWhiteSpace(request.Correo) || string.IsNullOrWhiteSpace(request.Password))
                return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "Correo y contraseña son obligatorios para crear el acceso.");
        }
        else if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, request.NegocioId, cancellationToken))
        {
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");
        }

        if (request.SucursalId.HasValue && !await _empleados.SucursalValidaAsync(request.NegocioId, request.SucursalId.Value, cancellationToken))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "La sucursal no pertenece a este negocio.");

        var empleado = new Empleado
        {
            NegocioId = request.NegocioId,
            SucursalId = request.SucursalId,
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Telefono = Normalize(request.Telefono),
            Correo = Normalize(request.Correo),
            Puesto = Normalize(request.Puesto),
            Biografia = Normalize(request.Biografia),
            Estado = request.Activo ? "active" : "inactive",
            CreadoEn = DateTime.UtcNow,
        };

        if (request.CrearAcceso)
        {
            var correo = request.Correo!.Trim().ToLowerInvariant();
            if (await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken) is not null)
                return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Conflict, "El correo ya se encuentra registrado.");

            var usuario = new Usuario { Nombre = empleado.Nombre, Apellido = empleado.Apellido, Correo = correo, Estado = "active", CreadoEn = DateTime.UtcNow };
            usuario.ContrasenaHash = _passwordHasher.HashPassword(usuario, request.Password!);
            var miembro = new MiembrosNegocio { NegocioId = request.NegocioId, Usuario = usuario, RolMiembro = "employee", Estado = "active", CreadoEn = DateTime.UtcNow };

            await _empleados.AgregarConUsuarioAsync(empleado, usuario, miembro, cancellationToken);
        }
        else
        {
            await _empleados.AgregarAsync(empleado, cancellationToken);
            await _empleados.GuardarCambiosAsync(cancellationToken);
        }

        return await ReloadAsync(empleado.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<EmpleadoResponse>> ActualizarAsync(long usuarioId, long id, GuardarEmpleadoRequest request, CancellationToken cancellationToken = default)
    {
        var empleado = await _empleados.ObtenerAsync(id, true, cancellationToken);
        if (empleado is null) return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.NotFound, "El empleado no existe.");
        if (empleado.NegocioId != request.NegocioId) return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "No se puede mover el empleado a otro negocio.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, request.NegocioId, cancellationToken))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");
        if (request.SucursalId.HasValue && !await _empleados.SucursalValidaAsync(request.NegocioId, request.SucursalId.Value, cancellationToken))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "La sucursal no pertenece a este negocio.");

        empleado.SucursalId = request.SucursalId;
        empleado.Nombre = request.Nombre.Trim();
        empleado.Apellido = request.Apellido.Trim();
        empleado.Telefono = Normalize(request.Telefono);
        empleado.Correo = Normalize(request.Correo);
        empleado.Puesto = Normalize(request.Puesto);
        empleado.Biografia = Normalize(request.Biografia);
        empleado.Estado = request.Activo ? "active" : "inactive";
        empleado.ActualizadoEn = DateTime.UtcNow;

        await _empleados.GuardarCambiosAsync(cancellationToken);
        return await ReloadAsync(empleado.Id, cancellationToken);
    }

    public async Task<MaintenanceResult<bool>> EliminarAsync(long usuarioId, long id, CancellationToken cancellationToken = default)
    {
        var empleado = await _empleados.ObtenerAsync(id, true, cancellationToken);
        if (empleado is null) return MaintenanceResult<bool>.Fail(MaintenanceStatus.NotFound, "El empleado no existe.");
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, empleado.NegocioId, cancellationToken))
            return MaintenanceResult<bool>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este empleado.");

        empleado.Estado = "inactive";
        empleado.ActualizadoEn = DateTime.UtcNow;
        await _empleados.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<MaintenanceResult<EmpleadoResponse>> ReloadAsync(long id, CancellationToken cancellationToken)
    {
        var empleado = await _empleados.ObtenerAsync(id, false, cancellationToken);
        return empleado is null ? MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.NotFound, "No se pudo recargar el empleado.") : MaintenanceResult<EmpleadoResponse>.Ok(Map(empleado));
    }

    private static EmpleadoResponse Map(Empleado item) => new(
        item.Id, item.NegocioId, item.SucursalId, item.Sucursal?.Nombre,
        item.Nombre, item.Apellido, item.Telefono, item.Correo,
        item.Puesto, item.Biografia, item.FotoUrl, item.Estado, item.UsuarioId.HasValue);
}
