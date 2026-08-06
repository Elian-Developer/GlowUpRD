using GlowUpRD.API.DTOs.Empleados;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using GlowUpRD.API.Validation;
using GlowUpRD.API.Data;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Services.Implementations;

public sealed class EmpleadoService : IEmpleadoService
{
    private readonly IEmpleadoRepository _empleados;
    private readonly INegocioRepository _negocios;
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly GlowUpDbContext _context;

    public EmpleadoService(IEmpleadoRepository empleados, INegocioRepository negocios, IUsuarioRepository usuarios, IPasswordHasher<Usuario> passwordHasher, GlowUpDbContext context)
    {
        _empleados = empleados;
        _negocios = negocios;
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
        _context = context;
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
            if (!string.Equals(request.Password, request.ConfirmarPassword, StringComparison.Ordinal))
                return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "confirmarPassword", "PASSWORD_CONFIRMATION_MISMATCH", "Las contraseñas no coinciden.");
        }
        else if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, request.NegocioId, cancellationToken))
        {
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");
        }

        var sucursalIds = await ResolverSucursalesAsync(request, cancellationToken);
        if (sucursalIds.Count == 0)
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "Selecciona al menos una sucursal activa del negocio.");
        if (!await ServiciosValidosAsync(request.NegocioId, request.ServicioIds, cancellationToken))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "servicioIds", "INVALID_SERVICES", "Selecciona únicamente servicios activos de este negocio.");
        if (request.CrearAcceso && !InputRules.IsPasswordStrong(request.Password, request.Correo, request.Telefono, request.Nombre, request.Apellido))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "password", "WEAK_PASSWORD", InputRules.PasswordMessage);

        var empleado = new Empleado
        {
            NegocioId = request.NegocioId,
            SucursalId = sucursalIds[0],
            Nombre = InputNormalizer.RequiredText(request.Nombre),
            Apellido = InputNormalizer.RequiredText(request.Apellido),
            Telefono = InputNormalizer.NormalizePhone(request.Telefono),
            Correo = string.IsNullOrWhiteSpace(request.Correo) ? null : InputNormalizer.NormalizeEmail(request.Correo),
            Puesto = Normalize(request.Puesto),
            Biografia = Normalize(request.Biografia),
            Estado = request.Activo ? "active" : "inactive",
            CreadoEn = DateTime.UtcNow,
        };
        var horarios = request.Horarios.Count == 0
            ? (await Task.WhenAll(sucursalIds.Select(sucursalId => HorariosDesdeNegocioAsync(request.NegocioId, sucursalId, cancellationToken)))).SelectMany(item => item).ToList()
            : CompletarSucursalEnHorarios(request.Horarios, sucursalIds);
        if (!HorariosValidos(horarios))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "Configura un horario único y válido para cada día del empleado.");
        if (!await HorariosDentroDelNegocioAsync(request.NegocioId, horarios, cancellationToken))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "horarios", "OUTSIDE_BUSINESS_HOURS", "Los turnos del empleado deben estar dentro del horario abierto de cada sucursal.");
        empleado.HorariosEmpleados = CrearHorarios(empleado.Id, horarios);
        empleado.EmpleadosSucursales = sucursalIds.Select(sucursalId => new EmpleadoSucursal { SucursalId = sucursalId, Estado = "active", CreadoEn = DateTime.UtcNow }).ToList();
        empleado.ServiciosEmpleados = CrearServicios(empleado.Id, request.ServicioIds);

        if (request.CrearAcceso)
        {
            var correo = InputNormalizer.NormalizeEmail(request.Correo);
            if (await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken) is not null)
                return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Conflict, "correo", "DUPLICATE_EMAIL", "Ya existe una cuenta registrada con este correo.");

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
        var sucursalIds = await ResolverSucursalesAsync(request, cancellationToken);
        if (sucursalIds.Count == 0)
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "Selecciona al menos una sucursal activa del negocio.");
        if (!await ServiciosValidosAsync(request.NegocioId, request.ServicioIds, cancellationToken))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "servicioIds", "INVALID_SERVICES", "Selecciona únicamente servicios activos de este negocio.");

        empleado.SucursalId = sucursalIds[0];
        empleado.Nombre = InputNormalizer.RequiredText(request.Nombre);
        empleado.Apellido = InputNormalizer.RequiredText(request.Apellido);
        empleado.Telefono = InputNormalizer.NormalizePhone(request.Telefono);
        empleado.Correo = string.IsNullOrWhiteSpace(request.Correo) ? null : InputNormalizer.NormalizeEmail(request.Correo);
        empleado.Puesto = Normalize(request.Puesto);
        empleado.Biografia = Normalize(request.Biografia);
        empleado.Estado = request.Activo ? "active" : "inactive";
        empleado.ActualizadoEn = DateTime.UtcNow;
        var horarios = CompletarSucursalEnHorarios(request.Horarios, sucursalIds);
        if (!HorariosValidos(horarios))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "Configura un horario único y válido para cada día del empleado.");
        if (!await HorariosDentroDelNegocioAsync(request.NegocioId, horarios, cancellationToken))
            return MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.Invalid, "horarios", "OUTSIDE_BUSINESS_HOURS", "Los turnos del empleado deben estar dentro del horario abierto de cada sucursal.");
        _empleados.ReemplazarHorarios(empleado, CrearHorarios(empleado.Id, horarios));
        _empleados.ReemplazarSucursales(empleado, sucursalIds.Select(sucursalId => new EmpleadoSucursal { EmpleadoId = empleado.Id, SucursalId = sucursalId, Estado = "active", CreadoEn = DateTime.UtcNow }));
        _empleados.ReemplazarServicios(empleado, CrearServicios(empleado.Id, request.ServicioIds));

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
        empleado.EliminadoEn = DateTime.UtcNow;
        empleado.ActualizadoEn = DateTime.UtcNow;
        await _empleados.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    public async Task<int> CompletarHorariosPendientesAsync(CancellationToken cancellationToken = default)
    {
        var empleados = await _empleados.ObtenerTodosParaHorariosAsync(cancellationToken);
        var actualizados = 0;
        foreach (var empleado in empleados)
        {
            var actualizado = false;
            if (empleado.HorariosEmpleados.Any(item => !item.Activo))
            {
                _empleados.EliminarHorariosInactivos(empleado);
                actualizado = true;
            }

            if (!empleado.HorariosEmpleados.Any(item => item.Activo))
            {
                var sucursalId = empleado.EmpleadosSucursales.FirstOrDefault(item => item.Estado == "active")?.SucursalId ?? empleado.SucursalId;
                if (!sucursalId.HasValue) continue;
                var horarios = await HorariosDesdeNegocioAsync(empleado.NegocioId, sucursalId.Value, cancellationToken);
                if (horarios.Count > 0 && HorariosValidos(horarios))
                {
                    foreach (var horario in CrearHorarios(empleado.Id, horarios)) empleado.HorariosEmpleados.Add(horario);
                    actualizado = true;
                }
            }

            if (actualizado) actualizados++;
        }
        if (actualizados > 0) await _empleados.GuardarCambiosAsync(cancellationToken);
        return actualizados;
    }

    private static string? Normalize(string? value) => InputNormalizer.OptionalText(value);

    private async Task<bool> ServiciosValidosAsync(long negocioId, IEnumerable<long>? servicioIds, CancellationToken cancellationToken)
    {
        var solicitados = (servicioIds ?? []).ToList();
        var distintos = solicitados.Where(id => id > 0).Distinct().ToList();
        if (distintos.Count != solicitados.Count) return false;
        if (distintos.Count == 0) return true;
        var encontrados = await _context.Servicios.AsNoTracking()
            .CountAsync(servicio => servicio.NegocioId == negocioId && servicio.Activo && servicio.EliminadoEn == null && distintos.Contains(servicio.Id), cancellationToken);
        return encontrados == distintos.Count;
    }

    private async Task<MaintenanceResult<EmpleadoResponse>> ReloadAsync(long id, CancellationToken cancellationToken)
    {
        var empleado = await _empleados.ObtenerAsync(id, false, cancellationToken);
        return empleado is null ? MaintenanceResult<EmpleadoResponse>.Fail(MaintenanceStatus.NotFound, "No se pudo recargar el empleado.") : MaintenanceResult<EmpleadoResponse>.Ok(Map(empleado));
    }

    private async Task<List<long>> ResolverSucursalesAsync(GuardarEmpleadoRequest request, CancellationToken cancellationToken)
    {
        var solicitadas = request.SucursalIds.Where(id => id > 0).Distinct().ToList();
        if (solicitadas.Count == 0 && request.SucursalId.HasValue) solicitadas.Add(request.SucursalId.Value);
        if (solicitadas.Count == 0)
        {
            var principal = await _context.Sucursales.AsNoTracking()
                .Where(item => item.NegocioId == request.NegocioId && item.EsPrincipal && item.Estado == "active")
                .Select(item => (long?)item.Id).SingleOrDefaultAsync(cancellationToken);
            if (principal.HasValue) solicitadas.Add(principal.Value);
        }
        var validas = await _context.Sucursales.AsNoTracking()
            .Where(item => item.NegocioId == request.NegocioId && item.Estado == "active" && solicitadas.Contains(item.Id))
            .Select(item => item.Id).ToListAsync(cancellationToken);
        return validas.Count == solicitadas.Count ? validas.OrderBy(item => item).ToList() : [];
    }

    private static List<GuardarHorarioEmpleadoRequest> CompletarSucursalEnHorarios(IEnumerable<GuardarHorarioEmpleadoRequest> horarios, IReadOnlyList<long> sucursalIds)
    {
        var activos = HorariosActivos(horarios);
        if (activos.Any(item => !item.SucursalId.HasValue) && sucursalIds.Count != 1) return [];
        return activos.Where(item => !item.SucursalId.HasValue || sucursalIds.Contains(item.SucursalId.Value)).Select(item => new GuardarHorarioEmpleadoRequest
        {
            SucursalId = item.SucursalId ?? sucursalIds[0], DiaSemana = item.DiaSemana,
            IniciaA = item.IniciaA, TerminaA = item.TerminaA, Activo = true
        }).ToList();
    }

    private async Task<List<GuardarHorarioEmpleadoRequest>> HorariosDesdeNegocioAsync(long negocioId, long sucursalId, CancellationToken cancellationToken)
    {
        var horarios = await _context.HorariosNegocios.AsNoTracking()
            .Where(item => item.SucursalId == sucursalId && item.Sucursal.NegocioId == negocioId)
            .OrderBy(item => item.DiaSemana).ToListAsync(cancellationToken);
        return Enumerable.Range(0, 7).Select(dia =>
        {
            var horario = horarios.SingleOrDefault(item => item.DiaSemana == dia);
            return horario is null || horario.Cerrado || !horario.AbreA.HasValue || !horario.CierraA.HasValue
                ? null
                : new GuardarHorarioEmpleadoRequest { SucursalId = sucursalId, DiaSemana = (short)dia, Activo = true, IniciaA = horario.AbreA, TerminaA = horario.CierraA };
        }).Where(item => item is not null).Cast<GuardarHorarioEmpleadoRequest>().ToList();
    }

    private async Task<bool> HorariosDentroDelNegocioAsync(long negocioId, IReadOnlyCollection<GuardarHorarioEmpleadoRequest> horarios, CancellationToken cancellationToken)
    {
        if (horarios.Count == 0) return true;
        var sucursalIds = horarios.Select(item => item.SucursalId!.Value).Distinct().ToList();
        var horariosNegocio = await _context.HorariosNegocios.AsNoTracking()
            .Where(item => item.Sucursal.NegocioId == negocioId && sucursalIds.Contains(item.SucursalId))
            .ToListAsync(cancellationToken);
        return horarios.All(turno =>
        {
            var horarioNegocio = horariosNegocio.SingleOrDefault(item => item.SucursalId == turno.SucursalId && item.DiaSemana == turno.DiaSemana);
            return horarioNegocio is not null && !horarioNegocio.Cerrado && horarioNegocio.AbreA.HasValue && horarioNegocio.CierraA.HasValue &&
                turno.IniciaA >= horarioNegocio.AbreA && turno.TerminaA <= horarioNegocio.CierraA;
        });
    }

    private static bool HorariosValidos(IReadOnlyCollection<GuardarHorarioEmpleadoRequest> horarios) =>
        horarios.All(item => item.Activo && item.IniciaA.HasValue && item.TerminaA.HasValue && item.IniciaA < item.TerminaA) &&
        horarios.All(item => item.SucursalId.HasValue) &&
        horarios.GroupBy(item => new { item.SucursalId, item.DiaSemana }).All(turnos => turnos.OrderBy(item => item.IniciaA).Zip(turnos.OrderBy(item => item.IniciaA).Skip(1), (actual, siguiente) => actual.TerminaA <= siguiente.IniciaA).All(valido => valido));

    private static List<GuardarHorarioEmpleadoRequest> HorariosActivos(IEnumerable<GuardarHorarioEmpleadoRequest> horarios) => horarios.Where(item => item.Activo).ToList();

    private static List<HorariosEmpleado> CrearHorarios(long empleadoId, IEnumerable<GuardarHorarioEmpleadoRequest> horarios) => HorariosActivos(horarios).Select(item => new HorariosEmpleado
    {
        EmpleadoId = empleadoId, SucursalId = item.SucursalId!.Value, DiaSemana = item.DiaSemana, Activo = true,
        IniciaA = item.IniciaA ?? TimeOnly.MinValue, TerminaA = item.TerminaA ?? TimeOnly.MinValue,
    }).ToList();

    private static List<ServiciosEmpleado> CrearServicios(long empleadoId, IEnumerable<long>? servicioIds) => (servicioIds ?? [])
        .Distinct().Select(servicioId => new ServiciosEmpleado { EmpleadoId = empleadoId, ServicioId = servicioId, CreadoEn = DateTime.UtcNow }).ToList();

    private static EmpleadoResponse Map(Empleado item) => new(
        item.Id, item.NegocioId, item.SucursalId, item.Sucursal?.Nombre,
        item.Nombre, item.Apellido, item.Telefono, item.Correo,
        item.Puesto, item.Biografia, item.FotoUrl, item.Estado, item.UsuarioId.HasValue,
        item.HorariosEmpleados.Where(horario => horario.Activo).OrderBy(horario => horario.SucursalId).ThenBy(horario => horario.DiaSemana)
            .Select(horario => new HorarioEmpleadoResponse(horario.SucursalId, horario.DiaSemana,
                horario.IniciaA, horario.TerminaA, true)).ToList(),
        item.EmpleadosSucursales.Where(sucursal => sucursal.Estado == "active")
            .OrderBy(sucursal => sucursal.Sucursal.Nombre)
            .Select(sucursal => new EmpleadoSucursalResponse(sucursal.SucursalId, sucursal.Sucursal.Nombre, sucursal.Estado)).ToList(),
        item.ServiciosEmpleados.Select(servicio => servicio.ServicioId).OrderBy(id => id).ToList());
}
