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

namespace GlowUpRD.API.Services.Implementations;

public sealed class NegocioService : INegocioService
{
    private static readonly HashSet<string> TiposValidos = ["salon", "barbershop", "spa", "mixed"];
    private static readonly HashSet<string> RolesAsignables = ["manager", "employee", "receptionist"];

    private readonly INegocioRepository _negocios;
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IDemoDataService _demoData;
    private readonly GlowUpDbContext _context;

    public NegocioService(
        INegocioRepository negocios,
        IUsuarioRepository usuarios,
        IPasswordHasher<Usuario> passwordHasher,
        ITokenService tokenService,
        IDemoDataService demoData,
        GlowUpDbContext context)
    {
        _negocios = negocios;
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _demoData = demoData;
        _context = context;
    }

    public async Task<MaintenanceResult<LoginResponse>> RegistrarAsync(RegistrarNegocioRequest request, CancellationToken cancellationToken = default)
    {
        if (!TiposValidos.Contains(request.TipoNegocio))
            return MaintenanceResult<LoginResponse>.Fail(MaintenanceStatus.Invalid, "El tipo de negocio no es válido.");

        var correo = request.CorreoPropietario.Trim().ToLowerInvariant();
        if (await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken) is not null)
            return MaintenanceResult<LoginResponse>.Fail(MaintenanceStatus.Conflict, "El correo ya se encuentra registrado.");

        var slug = await GenerarSlugUnicoAsync(request.Nombre, cancellationToken);

        var negocio = new Negocio
        {
            Nombre = request.Nombre.Trim(),
            Slug = slug,
            TipoNegocio = request.TipoNegocio,
            Descripcion = Normalize(request.Descripcion),
            Rnc = Normalize(request.Rnc),
            Telefono = Normalize(request.Telefono),
            Correo = Normalize(request.Correo),
            Estado = "active",
            CreadoEn = DateTime.UtcNow,
        };
        negocio.Sucursales.Add(new Sucursal
        {
            Nombre = "Sucursal principal",
            Direccion = request.Direccion.Trim(),
            Ciudad = request.Ciudad.Trim(),
            Provincia = request.Provincia.Trim(),
            Pais = "República Dominicana",
            EsPrincipal = true,
            Estado = "active",
            CreadoEn = DateTime.UtcNow,
        });

        var propietario = new Usuario
        {
            Nombre = request.NombrePropietario.Trim(),
            Apellido = request.ApellidoPropietario.Trim(),
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

        return MaintenanceResult<LoginResponse>.Ok(_tokenService.CrearToken(propietario));
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

        if (!HorariosValidos(request.Horarios))
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Invalid, "Configura un horario único y válido para cada día de la semana.");
        if (!FeriadosValidos(request.Feriados))
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Invalid, "Cada festivo debe tener una fecha única desde hoy y un nombre válido.");

        var sucursalPrincipal = negocio.Sucursales
            .Where(sucursal => sucursal.EsPrincipal)
            .OrderBy(sucursal => sucursal.Id)
            .FirstOrDefault();
        if (sucursalPrincipal is null)
            return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Invalid, "El negocio no tiene una sucursal principal configurada.");

        foreach (var feriado in request.Feriados.Where(item => negocio.FeriadosNegocios.All(actual => actual.Fecha != item.Fecha)))
        {
            if (feriado.Fecha < DateOnly.FromDateTime(DateTime.Today))
                return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Invalid, "Solo puedes agregar festivos desde hoy en adelante.");
            if (await _negocios.TieneCitasBloqueantesEnFechaAsync(negocioId, feriado.Fecha, cancellationToken))
                return MaintenanceResult<NegocioDetalleResponse>.Fail(MaintenanceStatus.Conflict, $"No puedes cerrar el {feriado.Fecha:yyyy-MM-dd} porque todavía tiene citas que debes reprogramar o cancelar.");
        }

        negocio.Nombre = request.Nombre.Trim();
        negocio.Rnc = Normalize(request.Rnc);
        negocio.Telefono = Normalize(request.Telefono);
        negocio.Correo = Normalize(request.Correo);
        negocio.Descripcion = Normalize(request.Descripcion);
        negocio.LogoUrl = Normalize(request.LogoUrl);
        negocio.ActualizadoEn = DateTime.UtcNow;

        sucursalPrincipal.Nombre = request.SucursalPrincipal.Nombre.Trim();
        sucursalPrincipal.Telefono = Normalize(request.SucursalPrincipal.Telefono);
        sucursalPrincipal.Direccion = request.SucursalPrincipal.Direccion.Trim();
        sucursalPrincipal.Ciudad = request.SucursalPrincipal.Ciudad.Trim();
        sucursalPrincipal.Provincia = request.SucursalPrincipal.Provincia.Trim();
        sucursalPrincipal.Pais = request.SucursalPrincipal.Pais.Trim();
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
        _negocios.EliminarFeriados(negocio.FeriadosNegocios.Where(item => !fechasSolicitadas.Contains(item.Fecha)).ToList());
        foreach (var feriadoRequest in request.Feriados)
        {
            var feriado = negocio.FeriadosNegocios.SingleOrDefault(item => item.Fecha == feriadoRequest.Fecha);
            if (feriado is null)
            {
                feriado = new FeriadoNegocio { NegocioId = negocio.Id, Fecha = feriadoRequest.Fecha, CreadoEn = DateTime.UtcNow };
                negocio.FeriadosNegocios.Add(feriado);
            }
            feriado.Nombre = feriadoRequest.Nombre.Trim();
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

        var correo = request.Correo.Trim().ToLowerInvariant();
        if (await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken) is not null)
            return MaintenanceResult<MiembroNegocioResponse>.Fail(MaintenanceStatus.Conflict, "El correo ya se encuentra registrado.");

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
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

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool HorariosValidos(IReadOnlyCollection<ActualizarHorarioNegocioRequest> horarios) =>
        horarios.Count == 7 &&
        horarios.Select(horario => horario.DiaSemana).Distinct().Count() == 7 &&
        horarios.All(horario => horario.Cerrado ||
            (horario.AbreA.HasValue && horario.CierraA.HasValue && horario.AbreA < horario.CierraA));

    private static bool FeriadosValidos(IReadOnlyCollection<ActualizarFeriadoNegocioRequest> feriados) =>
        feriados.All(feriado => !string.IsNullOrWhiteSpace(feriado.Nombre)) &&
        feriados.Select(feriado => feriado.Fecha).Distinct().Count() == feriados.Count;

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
            negocio.FeriadosNegocios.OrderBy(feriado => feriado.Fecha)
                .Select(feriado => new FeriadoNegocioResponse(feriado.Id, feriado.Fecha, feriado.Nombre)).ToList());
    }

    private static MiembroNegocioResponse Map(MiembrosNegocio miembro) =>
        new(miembro.UsuarioId, miembro.Usuario.Nombre, miembro.Usuario.Apellido, miembro.Usuario.Correo, miembro.RolMiembro, miembro.Estado);

    private static MiembroNegocioResponse Map(MiembrosNegocio miembro, Usuario usuario) =>
        new(usuario.Id, usuario.Nombre, usuario.Apellido, usuario.Correo, miembro.RolMiembro, miembro.Estado);
}
