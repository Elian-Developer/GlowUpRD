using System.Globalization;
using System.Text;
using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.DTOs.Negocios;
using GlowUpRD.API.Data;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.Services.Implementations;

public sealed class NegocioService : INegocioService
{
    private static readonly HashSet<string> TiposValidos = ["salon", "barbershop", "spa", "mixed"];
    private static readonly HashSet<string> RolesAsignables = ["manager", "employee", "receptionist"];

    private readonly INegocioRepository _negocios;
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly ISessionService _sessions;
    private readonly IDemoDataService _demoData;
    private readonly GlowUpDbContext _context;

    public NegocioService(
        INegocioRepository negocios,
        IUsuarioRepository usuarios,
        IPasswordHasher<Usuario> passwordHasher,
        ISessionService sessions,
        IDemoDataService demoData,
        GlowUpDbContext context)
    {
        _negocios = negocios;
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
        _sessions = sessions;
        _demoData = demoData;
        _context = context;
    }

    public async Task<MaintenanceResult<SesionAutenticada>> RegistrarAsync(RegistrarNegocioRequest request, CancellationToken cancellationToken = default)
    {
        if (!TiposValidos.Contains(request.TipoNegocio))
            return MaintenanceResult<SesionAutenticada>.Fail(MaintenanceStatus.Invalid, "El tipo de negocio no es válido.");

        if (!string.Equals(request.Password, request.ConfirmarPassword, StringComparison.Ordinal))
            return MaintenanceResult<SesionAutenticada>.Fail(MaintenanceStatus.Invalid, "confirmarPassword", "PASSWORD_CONFIRMATION_MISMATCH", "Las contraseñas no coinciden.");
        if (!InputRules.IsPasswordStrong(request.Password, request.CorreoPropietario, request.Telefono, request.NombrePropietario, request.ApellidoPropietario, request.Nombre))
            return MaintenanceResult<SesionAutenticada>.Fail(MaintenanceStatus.Invalid, "password", "WEAK_PASSWORD", InputRules.PasswordMessage);

        var correo = InputNormalizer.NormalizeEmail(request.CorreoPropietario);
        if (await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken) is not null)
            return MaintenanceResult<SesionAutenticada>.Fail(MaintenanceStatus.Conflict, "El correo ya se encuentra registrado.");

        var slug = await GenerarSlugUnicoAsync(request.Nombre, cancellationToken);

        var negocio = new Negocio
        {
            Nombre = InputNormalizer.RequiredText(request.Nombre),
            Slug = slug,
            TipoNegocio = request.TipoNegocio,
            Descripcion = Normalize(request.Descripcion),
            Rnc = Normalize(request.Rnc),
            Telefono = InputNormalizer.NormalizePhone(request.Telefono),
            Correo = string.IsNullOrWhiteSpace(request.Correo) ? null : InputNormalizer.NormalizeEmail(request.Correo),
            Estado = "active",
            CreadoEn = DateTime.UtcNow,
        };
        negocio.Sucursales.Add(new Sucursal
        {
            Nombre = "Sucursal principal",
            Direccion = InputNormalizer.RequiredText(request.Direccion),
            Ciudad = InputNormalizer.RequiredText(request.Ciudad),
            Provincia = InputNormalizer.RequiredText(request.Provincia),
            Pais = "República Dominicana",
            EsPrincipal = true,
            Estado = "active",
            CreadoEn = DateTime.UtcNow,
        });

        var propietario = new Usuario
        {
            Nombre = InputNormalizer.RequiredText(request.NombrePropietario),
            Apellido = InputNormalizer.RequiredText(request.ApellidoPropietario),
            Correo = correo,
            Estado = "active",
            CreadoEn = DateTime.UtcNow,
        };
        propietario.ContrasenaHash = _passwordHasher.HashPassword(propietario, request.Password);
        negocio.UsuarioPropietario = propietario;

        var miembro = new MiembrosNegocio
        {
            Negocio = negocio,
            Usuario = propietario,
            RolMiembro = "owner",
            Estado = "active",
            CreadoEn = DateTime.UtcNow,
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await _negocios.RegistrarAsync(negocio, propietario, miembro, cancellationToken);
            await _demoData.ProvisionForBusinessAsync(negocio.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return MaintenanceResult<SesionAutenticada>.Ok(await _sessions.CrearAsync(propietario, request.RecordarSesion, cancellationToken));
    }

    public async Task<MaintenanceResult<NegocioDetalleResponse>> ObtenerAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var negocio = await _negocios.ObtenerPerfilAsync(negocioId, cancellationToken);
        if (negocio is null)
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.NotFound, "El negocio no existe.");
        if (!negocio.Sucursales.Any(sucursal => sucursal.EsPrincipal))
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Invalid, "El negocio no tiene una sucursal principal configurada.");

        return MaintenanceResult<NegocioDetalleResponse>.Ok(Map(negocio));
    }

    public async Task<MaintenanceResult<NegocioDetalleResponse>> ActualizarAsync(long usuarioId, long negocioId, ActualizarNegocioRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioEsPropietarioAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Forbidden, "Solo el dueño del negocio puede editar este perfil.");

        var negocio = await _negocios.ObtenerPerfilParaEditarAsync(negocioId, cancellationToken);
        if (negocio is null) return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.NotFound, "El negocio no existe.");

        var principal = await _context.Sucursales.AsNoTracking().Include(item => item.HorariosNegocios)
            .SingleOrDefaultAsync(item => item.NegocioId == negocioId && item.EsPrincipal && item.Estado == "active", cancellationToken);
        var horariosBase = request.Horarios.Count == 0 && principal is not null
            ? principal.HorariosNegocios.Select(item => new ActualizarHorarioNegocioRequest { DiaSemana = item.DiaSemana, Cerrado = item.Cerrado, AbreA = item.AbreA, CierraA = item.CierraA }).ToList()
            : request.Horarios;
        if (!HorariosValidos(horariosBase))
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Invalid, "Configura un horario único y válido para cada día de la semana.");
        if (!FeriadosValidos(request.Feriados))
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Invalid, "Cada festivo debe tener una fecha única desde hoy y un nombre válido.");

        var sucursalPrincipal = negocio.Sucursales
            .Where(sucursal => sucursal.EsPrincipal)
            .OrderBy(sucursal => sucursal.Id)
            .FirstOrDefault();
        if (sucursalPrincipal is null)
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Invalid, "El negocio no tiene una sucursal principal configurada.");

        foreach (var feriado in request.Feriados.Where(item => negocio.FeriadosNegocios.All(actual => actual.SucursalId == sucursalPrincipal.Id && actual.Fecha != item.Fecha)))
        {
            if (feriado.Fecha < DateOnly.FromDateTime(DateTime.Today))
                return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Invalid, "Solo puedes agregar festivos desde hoy en adelante.");
            if (await _negocios.TieneCitasBloqueantesEnFechaAsync(negocioId, feriado.Fecha, cancellationToken))
                return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Conflict, $"No puedes cerrar el {feriado.Fecha:yyyy-MM-dd} porque todavía tiene citas que debes reprogramar o cancelar.");
        }

        if (!InputRules.IsValidOptionalUrl(request.LogoUrl))
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Invalid, "logoUrl", "INVALID_URL", "La URL del logo debe empezar con http:// o https://.");

        negocio.Nombre = InputNormalizer.RequiredText(request.Nombre);
        negocio.Rnc = Normalize(request.Rnc);
        negocio.Telefono = InputNormalizer.NormalizePhone(request.Telefono);
        negocio.Correo = string.IsNullOrWhiteSpace(request.Correo) ? null : InputNormalizer.NormalizeEmail(request.Correo);
        negocio.Descripcion = Normalize(request.Descripcion);
        negocio.LogoUrl = Normalize(request.LogoUrl);
        negocio.ActualizadoEn = DateTime.UtcNow;

        sucursalPrincipal.Nombre = InputNormalizer.RequiredText(request.SucursalPrincipal.Nombre);
        sucursalPrincipal.Telefono = InputNormalizer.NormalizePhone(request.SucursalPrincipal.Telefono);
        sucursalPrincipal.Direccion = InputNormalizer.RequiredText(request.SucursalPrincipal.Direccion);
        sucursalPrincipal.Ciudad = InputNormalizer.RequiredText(request.SucursalPrincipal.Ciudad);
        sucursalPrincipal.Provincia = InputNormalizer.RequiredText(request.SucursalPrincipal.Provincia);
        sucursalPrincipal.Pais = InputNormalizer.RequiredText(request.SucursalPrincipal.Pais);
        sucursalPrincipal.ActualizadoEn = DateTime.UtcNow;

        foreach (var horarioRequest in request.Horarios)
        {
            var horario = sucursalPrincipal.HorariosNegocios
                .SingleOrDefault(item => item.DiaSemana == horarioRequest.DiaSemana);
            if (horario is null)
            {
                horario = new HorariosNegocio
                {
                    SucursalId = sucursalPrincipal.Id,
                    DiaSemana = horarioRequest.DiaSemana,
                };
                sucursalPrincipal.HorariosNegocios.Add(horario);
            }

            horario.Cerrado = horarioRequest.Cerrado;
            horario.AbreA = horarioRequest.Cerrado ? null : horarioRequest.AbreA;
            horario.CierraA = horarioRequest.Cerrado ? null : horarioRequest.CierraA;
        }

        var fechasSolicitadas = request.Feriados.Select(item => item.Fecha).ToHashSet();
        _negocios.EliminarFeriados(negocio.FeriadosNegocios.Where(item => item.SucursalId == sucursalPrincipal.Id && !fechasSolicitadas.Contains(item.Fecha)).ToList());
        foreach (var feriadoRequest in request.Feriados)
        {
            var feriado = negocio.FeriadosNegocios.SingleOrDefault(item => item.SucursalId == sucursalPrincipal.Id && item.Fecha == feriadoRequest.Fecha);
            if (feriado is null)
            {
                feriado = new FeriadoNegocio { NegocioId = negocio.Id, SucursalId = sucursalPrincipal.Id, Fecha = feriadoRequest.Fecha, CreadoEn = DateTime.UtcNow };
                negocio.FeriadosNegocios.Add(feriado);
            }
            feriado.Nombre = InputNormalizer.RequiredText(feriadoRequest.Nombre);
        }

        await _negocios.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<NegocioDetalleResponse>.Ok(Map(negocio));
    }

    public async Task<MaintenanceResult<IReadOnlyList<MiembroNegocioResponse>>> ObtenerMiembrosAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<IReadOnlyList<MiembroNegocioResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var miembros = await _negocios.ObtenerMiembrosAsync(negocioId, cancellationToken);
        return MaintenanceResult<IReadOnlyList<MiembroNegocioResponse>>.Ok(miembros.Select(Map).ToList());
    }

    public async Task<MaintenanceResult<MiembroNegocioResponse>> CrearUsuarioAsync(long usuarioId, long negocioId, CrearUsuarioNegocioRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioEsPropietarioAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<MiembroNegocioResponse>.Fail(MaintenanceStatus.Forbidden, "Solo el dueño del negocio puede crear nuevos usuarios.");

        if (!RolesAsignables.Contains(request.Rol))
            return MaintenanceResult<MiembroNegocioResponse>.Fail(MaintenanceStatus.Invalid, "El rol no es válido.");

        if (!InputRules.IsPasswordStrong(request.Password, request.Correo, request.Nombre, request.Apellido))
            return MaintenanceResult<MiembroNegocioResponse>.Fail(MaintenanceStatus.Invalid, "password", "WEAK_PASSWORD", InputRules.PasswordMessage);

        var correo = InputNormalizer.NormalizeEmail(request.Correo);
        if (await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken) is not null)
            return MaintenanceResult<MiembroNegocioResponse>.Fail(MaintenanceStatus.Conflict, "El correo ya se encuentra registrado.");

        var usuario = new Usuario
        {
            Nombre = InputNormalizer.RequiredText(request.Nombre),
            Apellido = InputNormalizer.RequiredText(request.Apellido),
            Correo = correo,
            Estado = "active",
            CreadoEn = DateTime.UtcNow,
        };
        usuario.ContrasenaHash = _passwordHasher.HashPassword(usuario, request.Password);

        var miembro = new MiembrosNegocio
        {
            NegocioId = negocioId,
            Usuario = usuario,
            RolMiembro = request.Rol,
            Estado = "active",
            CreadoEn = DateTime.UtcNow,
        };

        await _negocios.AgregarMiembroAsync(usuario, miembro, cancellationToken);
        return MaintenanceResult<MiembroNegocioResponse>.Ok(Map(miembro, usuario));
    }

    public async Task<MaintenanceResult<IReadOnlyList<SucursalResumenResponse>>> ObtenerSucursalesAsync(long usuarioId, long negocioId, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<IReadOnlyList<SucursalResumenResponse>>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");

        var sucursales = await _context.Sucursales.AsNoTracking()
            .Where(item => item.NegocioId == negocioId)
            .OrderByDescending(item => item.EsPrincipal).ThenBy(item => item.Nombre)
            .Select(item => new SucursalResumenResponse(item.Id, item.Nombre, item.Ciudad, item.EsPrincipal, item.Estado))
            .ToListAsync(cancellationToken);
        return MaintenanceResult<IReadOnlyList<SucursalResumenResponse>>.Ok(sucursales);
    }

    public async Task<MaintenanceResult<SucursalDetalleResponse>> ObtenerSucursalAsync(long usuarioId, long negocioId, long sucursalId, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioTieneAccesoAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.Forbidden, "No tienes acceso a este negocio.");
        var sucursal = await SucursalConDetalleAsync(negocioId, sucursalId, tracking: false, cancellationToken);
        return sucursal is null
            ? MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.NotFound, "La sucursal no existe.")
            : MaintenanceResult<SucursalDetalleResponse>.Ok(MapSucursal(sucursal));
    }

    public async Task<MaintenanceResult<SucursalDetalleResponse>> CrearSucursalAsync(long usuarioId, long negocioId, GuardarSucursalRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioEsPropietarioAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.Forbidden, "Solo el dueño del negocio puede administrar sucursales.");
        if (request.Horarios.Count > 0 && !HorariosValidos(request.Horarios))
            return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.Invalid, "Configura un horario único y válido para cada día de la semana.");

        var principal = await _context.Sucursales.AsNoTracking().Include(item => item.HorariosNegocios)
            .SingleOrDefaultAsync(item => item.NegocioId == negocioId && item.EsPrincipal && item.Estado == "active", cancellationToken);
        var horariosBase = request.Horarios.Count == 0 && principal is not null
            ? principal.HorariosNegocios.Select(item => new ActualizarHorarioNegocioRequest { DiaSemana = item.DiaSemana, Cerrado = item.Cerrado, AbreA = item.AbreA, CierraA = item.CierraA }).ToList()
            : request.Horarios;
        if (!HorariosValidos(horariosBase))
            return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.Invalid, "Configura un horario válido para cada día de la semana.");

        var sucursal = new Sucursal
        {
            NegocioId = negocioId, Nombre = InputNormalizer.RequiredText(request.Nombre),
            Telefono = InputNormalizer.NormalizePhone(request.Telefono), Direccion = InputNormalizer.RequiredText(request.Direccion),
            Ciudad = InputNormalizer.RequiredText(request.Ciudad), Provincia = InputNormalizer.RequiredText(request.Provincia),
            Pais = InputNormalizer.RequiredText(request.Pais), Estado = "active", EsPrincipal = false, CreadoEn = DateTime.UtcNow,
        };
        foreach (var horario in horariosBase)
            sucursal.HorariosNegocios.Add(new HorariosNegocio { DiaSemana = horario.DiaSemana, Cerrado = horario.Cerrado, AbreA = horario.Cerrado ? null : horario.AbreA, CierraA = horario.Cerrado ? null : horario.CierraA });

        _context.Sucursales.Add(sucursal);
        await _context.SaveChangesAsync(cancellationToken);
        return MaintenanceResult<SucursalDetalleResponse>.Ok(MapSucursal(sucursal));
    }

    public async Task<MaintenanceResult<SucursalDetalleResponse>> ActualizarSucursalAsync(long usuarioId, long negocioId, long sucursalId, GuardarSucursalRequest request, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioEsPropietarioAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.Forbidden, "Solo el dueño del negocio puede administrar sucursales.");
        if (!HorariosValidos(request.Horarios) || !FeriadosValidos(request.Feriados))
            return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.Invalid, "Revisa los horarios y festivos de la sucursal.");

        var sucursal = await SucursalConDetalleAsync(negocioId, sucursalId, tracking: true, cancellationToken);
        if (sucursal is null) return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.NotFound, "La sucursal no existe.");
        var targets = request.AplicarFeriadosATodas
            ? await _context.Sucursales.Where(item => item.NegocioId == negocioId && item.Estado == "active").Include(item => item.FeriadosNegocios).ToListAsync(cancellationToken)
            : [sucursal];
        var fechasNuevas = request.Feriados.Select(item => item.Fecha).ToHashSet();
        if (fechasNuevas.Any(fecha => fecha < DateOnly.FromDateTime(DateTime.Today)))
            return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.Invalid, "Solo puedes agregar festivos desde hoy en adelante.");

        foreach (var target in targets)
        {
            var fechasAgregadas = fechasNuevas.Except(target.FeriadosNegocios.Select(item => item.Fecha)).ToList();
            if (fechasAgregadas.Count == 0) continue;
            var bloqueada = await _context.Citas.AnyAsync(cita => cita.SucursalId == target.Id && cita.EliminadoEn == null && fechasAgregadas.Contains(cita.FechaCita) && (cita.Estado == "pending" || cita.Estado == "confirmed"), cancellationToken);
            if (bloqueada)
                return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.Conflict, "No puedes aplicar el festivo porque una sede tiene citas activas que debes reprogramar o cancelar.");
        }

        sucursal.Nombre = InputNormalizer.RequiredText(request.Nombre); sucursal.Telefono = InputNormalizer.NormalizePhone(request.Telefono);
        sucursal.Direccion = InputNormalizer.RequiredText(request.Direccion); sucursal.Ciudad = InputNormalizer.RequiredText(request.Ciudad);
        sucursal.Provincia = InputNormalizer.RequiredText(request.Provincia); sucursal.Pais = InputNormalizer.RequiredText(request.Pais); sucursal.ActualizadoEn = DateTime.UtcNow;
        foreach (var horarioRequest in request.Horarios)
        {
            var horario = sucursal.HorariosNegocios.SingleOrDefault(item => item.DiaSemana == horarioRequest.DiaSemana);
            if (horario is null) { horario = new HorariosNegocio { SucursalId = sucursal.Id, DiaSemana = horarioRequest.DiaSemana }; sucursal.HorariosNegocios.Add(horario); }
            horario.Cerrado = horarioRequest.Cerrado; horario.AbreA = horarioRequest.Cerrado ? null : horarioRequest.AbreA; horario.CierraA = horarioRequest.Cerrado ? null : horarioRequest.CierraA;
        }
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var target in targets) ReemplazarFeriados(target, request.Feriados);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return MaintenanceResult<SucursalDetalleResponse>.Ok(MapSucursal(sucursal));
    }

    public async Task<MaintenanceResult<bool>> DesactivarSucursalAsync(long usuarioId, long negocioId, long sucursalId, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioEsPropietarioAsync(usuarioId, negocioId, cancellationToken)) return MaintenanceResult<bool>.Fail(MaintenanceStatus.Forbidden, "Solo el dueño del negocio puede administrar sucursales.");
        var sucursal = await _context.Sucursales.SingleOrDefaultAsync(item => item.Id == sucursalId && item.NegocioId == negocioId, cancellationToken);
        if (sucursal is null) return MaintenanceResult<bool>.Fail(MaintenanceStatus.NotFound, "La sucursal no existe.");
        if (sucursal.EsPrincipal) return MaintenanceResult<bool>.Fail(MaintenanceStatus.Invalid, "No puedes desactivar la sucursal principal. Marca otra sede como principal primero.");
        if (await _context.Citas.AnyAsync(cita => cita.SucursalId == sucursalId && cita.EliminadoEn == null && cita.Inicio >= DateTime.Now && (cita.Estado == "pending" || cita.Estado == "confirmed"), cancellationToken)) return MaintenanceResult<bool>.Fail(MaintenanceStatus.Conflict, "Reprograma o cancela las citas futuras antes de desactivar esta sucursal.");
        sucursal.Estado = "inactive"; sucursal.ActualizadoEn = DateTime.UtcNow; await _context.SaveChangesAsync(cancellationToken); return MaintenanceResult<bool>.Ok(true);
    }

    public async Task<MaintenanceResult<SucursalDetalleResponse>> ReactivarSucursalAsync(long usuarioId, long negocioId, long sucursalId, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioEsPropietarioAsync(usuarioId, negocioId, cancellationToken))
            return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.Forbidden, "Solo el dueño del negocio puede administrar sucursales.");

        var sucursal = await SucursalConDetalleAsync(negocioId, sucursalId, tracking: true, cancellationToken);
        if (sucursal is null) return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.NotFound, "La sucursal no existe.");
        if (sucursal.Estado == "active") return MaintenanceResult<SucursalDetalleResponse>.Ok(MapSucursal(sucursal));

        sucursal.Estado = "active";
        sucursal.ActualizadoEn = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return MaintenanceResult<SucursalDetalleResponse>.Ok(MapSucursal(sucursal));
    }

    public async Task<MaintenanceResult<SucursalDetalleResponse>> MarcarSucursalPrincipalAsync(long usuarioId, long negocioId, long sucursalId, CancellationToken cancellationToken = default)
    {
        if (!await _negocios.UsuarioEsPropietarioAsync(usuarioId, negocioId, cancellationToken)) return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.Forbidden, "Solo el dueño del negocio puede administrar sucursales.");
        var sucursales = await _context.Sucursales.Where(item => item.NegocioId == negocioId).ToListAsync(cancellationToken);
        var nueva = sucursales.SingleOrDefault(item => item.Id == sucursalId && item.Estado == "active");
        if (nueva is null) return MaintenanceResult<SucursalDetalleResponse>.Fail(MaintenanceStatus.NotFound, "La sucursal activa no existe.");
        foreach (var sucursal in sucursales) sucursal.EsPrincipal = sucursal.Id == sucursalId;
        await _context.SaveChangesAsync(cancellationToken);
        var detalle = await SucursalConDetalleAsync(negocioId, sucursalId, tracking: false, cancellationToken);
        return MaintenanceResult<SucursalDetalleResponse>.Ok(MapSucursal(detalle!));
    }

    private async Task<string> GenerarSlugUnicoAsync(string nombre, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(nombre);
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "negocio";

        var slug = baseSlug;
        var sufijo = 2;
        while (await _negocios.SlugExisteAsync(slug, cancellationToken))
        {
            slug = $"{baseSlug}-{sufijo}";
            sufijo++;
        }
        return slug;
    }

    private static string Slugify(string valor)
    {
        var normalizado = valor.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var caracter in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(caracter)) builder.Append(char.ToLowerInvariant(caracter));
            else if (builder.Length > 0 && builder[^1] != '-') builder.Append('-');
        }
        return builder.ToString().Trim('-');
    }

    private static string? Normalize(string? value) => InputNormalizer.OptionalText(value);

    private static bool HorariosValidos(IReadOnlyCollection<ActualizarHorarioNegocioRequest> horarios) =>
        horarios.Count == 7 &&
        horarios.Select(horario => horario.DiaSemana).Distinct().Count() == 7 &&
        horarios.All(horario => horario.Cerrado ||
            (horario.AbreA.HasValue && horario.CierraA.HasValue && horario.AbreA < horario.CierraA));

    private static bool FeriadosValidos(IReadOnlyCollection<ActualizarFeriadoNegocioRequest> feriados) =>
        feriados.All(feriado => !string.IsNullOrWhiteSpace(feriado.Nombre)) &&
        feriados.Select(feriado => feriado.Fecha).Distinct().Count() == feriados.Count;

    private Task<Sucursal?> SucursalConDetalleAsync(long negocioId, long sucursalId, bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<Sucursal> query = _context.Sucursales
            .Include(item => item.HorariosNegocios)
            .Include(item => item.FeriadosNegocios);
        if (!tracking) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.Id == sucursalId && item.NegocioId == negocioId, cancellationToken);
    }

    private void ReemplazarFeriados(Sucursal sucursal, IReadOnlyCollection<ActualizarFeriadoNegocioRequest> feriados)
    {
        var fechas = feriados.Select(item => item.Fecha).ToHashSet();
        _context.FeriadosNegocios.RemoveRange(sucursal.FeriadosNegocios.Where(item => !fechas.Contains(item.Fecha)));
        foreach (var request in feriados)
        {
            var actual = sucursal.FeriadosNegocios.SingleOrDefault(item => item.Fecha == request.Fecha);
            if (actual is null)
            {
                actual = new FeriadoNegocio
                {
                    NegocioId = sucursal.NegocioId,
                    SucursalId = sucursal.Id,
                    Fecha = request.Fecha,
                    CreadoEn = DateTime.UtcNow
                };
                sucursal.FeriadosNegocios.Add(actual);
            }
            actual.Nombre = InputNormalizer.RequiredText(request.Nombre);
        }
    }

    private static SucursalDetalleResponse MapSucursal(Sucursal sucursal) => new(
        sucursal.Id, sucursal.NegocioId, sucursal.Nombre, sucursal.Telefono, sucursal.Direccion,
        sucursal.Ciudad, sucursal.Provincia, sucursal.Pais, sucursal.EsPrincipal, sucursal.Estado,
        sucursal.HorariosNegocios.OrderBy(item => item.DiaSemana)
            .Select(item => new HorarioNegocioResponse(item.DiaSemana, item.AbreA, item.CierraA, item.Cerrado)).ToList(),
        sucursal.FeriadosNegocios.OrderBy(item => item.Fecha)
            .Select(item => new FeriadoNegocioResponse(item.Id, item.Fecha, item.Nombre)).ToList());

    private static NegocioDetalleResponse Map(Negocio negocio)
    {
        var sucursalPrincipal = negocio.Sucursales
            .Where(sucursal => sucursal.EsPrincipal)
            .OrderBy(sucursal => sucursal.Id)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("El negocio no tiene una sucursal principal configurada.");

        return new NegocioDetalleResponse(
            negocio.Id, negocio.Nombre, negocio.Slug, negocio.TipoNegocio, negocio.Descripcion,
            negocio.Rnc, negocio.Telefono, negocio.Correo, negocio.LogoUrl, negocio.Estado,
            new SucursalPrincipalResponse(
                sucursalPrincipal.Id, sucursalPrincipal.Nombre, sucursalPrincipal.Telefono,
                sucursalPrincipal.Direccion, sucursalPrincipal.Ciudad, sucursalPrincipal.Provincia,
                sucursalPrincipal.Pais),
            sucursalPrincipal.HorariosNegocios
                .OrderBy(horario => horario.DiaSemana)
                .Select(horario => new HorarioNegocioResponse(
                    horario.DiaSemana, horario.AbreA, horario.CierraA, horario.Cerrado))
                .ToList(),
            negocio.FeriadosNegocios.Where(feriado => feriado.SucursalId == sucursalPrincipal.Id).OrderBy(feriado => feriado.Fecha)
                .Select(feriado => new FeriadoNegocioResponse(feriado.Id, feriado.Fecha, feriado.Nombre)).ToList(),
            negocio.Sucursales.OrderByDescending(sucursal => sucursal.EsPrincipal).ThenBy(sucursal => sucursal.Nombre)
                .Select(sucursal => new SucursalResumenResponse(sucursal.Id, sucursal.Nombre, sucursal.Ciudad, sucursal.EsPrincipal, sucursal.Estado)).ToList());
    }

    private static MiembroNegocioResponse Map(MiembrosNegocio miembro) =>
        new(miembro.UsuarioId, miembro.Usuario.Nombre, miembro.Usuario.Apellido, miembro.Usuario.Correo, miembro.RolMiembro, miembro.Estado);

    private static MiembroNegocioResponse Map(MiembrosNegocio miembro, Usuario usuario) =>
        new(usuario.Id, usuario.Nombre, usuario.Apellido, usuario.Correo, miembro.RolMiembro, miembro.Estado);
}
