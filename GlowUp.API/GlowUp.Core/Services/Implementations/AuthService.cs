using System.IdentityModel.Tokens.Jwt;
using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using GlowUpRD.API.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using GlowUpRD.API.Validation;
using GlowUpRD.API.Data;
using Microsoft.EntityFrameworkCore;

namespace GlowUpRD.API.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessions;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly GlowUpDbContext _context;

    public AuthService(
        IUsuarioRepository usuarios,
        IPasswordHasher<Usuario> passwordHasher,
        ITokenService tokenService,
        ISessionService sessions,
        IEmailSender emailSender,
        IConfiguration configuration,
        GlowUpDbContext context)
    {
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _sessions = sessions;
        _emailSender = emailSender;
        _configuration = configuration;
        _context = context;
    }

    public async Task<SesionAutenticada?> IniciarSesionAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorCorreoAsync(
            NormalizarCorreo(request.Correo), cancellationToken);

        if (usuario is null || usuario.Estado != "active")
        {
            return null;
        }

        var verificacion = _passwordHasher.VerifyHashedPassword(
            usuario, usuario.ContrasenaHash, request.Password);

        if (verificacion == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.ContrasenaHash = _passwordHasher.HashPassword(usuario, request.Password);
            usuario.ActualizadoEn = DateTime.UtcNow;
            await _usuarios.GuardarCambiosAsync(cancellationToken);
        }

        usuario.UltimoLoginEn = DateTime.UtcNow;
        await _usuarios.GuardarCambiosAsync(cancellationToken);

        return await _sessions.CrearAsync(usuario, request.RecordarSesion, cancellationToken);
    }

    public async Task<UsuarioResponse?> ObtenerPorIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(id, cancellationToken);
        return usuario is null ? null : Mapear(usuario);
    }

    public async Task<ActualizarUsuarioResultado> ActualizarAsync(
        long id,
        ActualizarUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return new(ActualizarUsuarioEstado.NoEncontrado);
        }

        var correo = NormalizarCorreo(request.Correo);
        var propietarioCorreo = await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken);
        if (propietarioCorreo is not null && propietarioCorreo.Id != id)
        {
            return new(ActualizarUsuarioEstado.CorreoDuplicado);
        }

        usuario.Nombre = InputNormalizer.RequiredText(request.Nombre);
        usuario.Apellido = InputNormalizer.RequiredText(request.Apellido);
        usuario.Correo = correo;
        usuario.ActualizadoEn = DateTime.UtcNow;

        await _usuarios.GuardarCambiosAsync(cancellationToken);
        return new(ActualizarUsuarioEstado.Exitoso, Mapear(usuario));
    }

    public async Task<bool> DesactivarAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return false;
        }

        usuario.Estado = "inactive";
        usuario.ActualizadoEn = DateTime.UtcNow;
        await _usuarios.GuardarCambiosAsync(cancellationToken);
        await _sessions.RevocarTodasDelUsuarioAsync(id, cancellationToken);
        return true;
    }

    public async Task<bool> EliminarCuentaAsync(long id, CancellationToken cancellationToken = default)
    {
        if (!await _context.Usuarios.AnyAsync(usuario => usuario.Id == id, cancellationToken)) return false;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var negocioIds = await _context.Negocios.Where(negocio => negocio.UsuarioPropietarioId == id)
            .Select(negocio => negocio.Id).ToListAsync(cancellationToken);
        var sucursalIds = await _context.Sucursales.Where(sucursal => negocioIds.Contains(sucursal.NegocioId))
            .Select(sucursal => sucursal.Id).ToListAsync(cancellationToken);
        var citaIds = await _context.Citas.Where(cita => negocioIds.Contains(cita.NegocioId))
            .Select(cita => cita.Id).ToListAsync(cancellationToken);
        var empleadoIds = await _context.Empleados.Where(empleado => negocioIds.Contains(empleado.NegocioId))
            .Select(empleado => empleado.Id).ToListAsync(cancellationToken);
        var clienteIds = await _context.ClientesNegocios.Where(cliente => negocioIds.Contains(cliente.NegocioId))
            .Select(cliente => cliente.ClienteId).Distinct().ToListAsync(cancellationToken);
        var servicioIds = await _context.Servicios.Where(servicio => negocioIds.Contains(servicio.NegocioId))
            .Select(servicio => servicio.Id).ToListAsync(cancellationToken);

        await _context.Notificaciones.Where(item => item.UsuarioId == id || negocioIds.Contains(item.NegocioId ?? 0) || citaIds.Contains(item.CitaId ?? 0)).ExecuteDeleteAsync(cancellationToken);
        await _context.RegistrosAuditoria.Where(item => item.UsuarioId == id || negocioIds.Contains(item.NegocioId ?? 0)).ExecuteDeleteAsync(cancellationToken);
        await _context.Pagos.Where(item => citaIds.Contains(item.CitaId)).ExecuteDeleteAsync(cancellationToken);
        await _context.Resenas.Where(item => citaIds.Contains(item.CitaId) || negocioIds.Contains(item.NegocioId)).ExecuteDeleteAsync(cancellationToken);
        await _context.ServiciosCita.Where(item => citaIds.Contains(item.CitaId)).ExecuteDeleteAsync(cancellationToken);
        await _context.Citas.Where(item => negocioIds.Contains(item.NegocioId)).ExecuteDeleteAsync(cancellationToken);
        await _context.ServiciosEmpleados.Where(item => empleadoIds.Contains(item.EmpleadoId) || servicioIds.Contains(item.ServicioId)).ExecuteDeleteAsync(cancellationToken);
        await _context.AusenciasEmpleados.Where(item => empleadoIds.Contains(item.EmpleadoId)).ExecuteDeleteAsync(cancellationToken);
        await _context.HorariosEmpleados.Where(item => empleadoIds.Contains(item.EmpleadoId)).ExecuteDeleteAsync(cancellationToken);
        await _context.EmpleadosSucursales.Where(item => empleadoIds.Contains(item.EmpleadoId) || sucursalIds.Contains(item.SucursalId)).ExecuteDeleteAsync(cancellationToken);
        await _context.Empleados.Where(item => negocioIds.Contains(item.NegocioId)).ExecuteDeleteAsync(cancellationToken);
        await _context.ClientesNegocios.Where(item => negocioIds.Contains(item.NegocioId)).ExecuteDeleteAsync(cancellationToken);
        await _context.Servicios.Where(item => negocioIds.Contains(item.NegocioId)).ExecuteDeleteAsync(cancellationToken);
        await _context.CategoriasServicios.Where(item => negocioIds.Contains(item.NegocioId)).ExecuteDeleteAsync(cancellationToken);
        await _context.MiembrosNegocios.Where(item => negocioIds.Contains(item.NegocioId) || item.UsuarioId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.SuscripcionesNegocios.Where(item => negocioIds.Contains(item.NegocioId)).ExecuteDeleteAsync(cancellationToken);
        await _context.FeriadosNegocios.Where(item => negocioIds.Contains(item.NegocioId) || sucursalIds.Contains(item.SucursalId)).ExecuteDeleteAsync(cancellationToken);
        await _context.HorariosNegocios.Where(item => sucursalIds.Contains(item.SucursalId)).ExecuteDeleteAsync(cancellationToken);
        await _context.Sucursales.Where(item => negocioIds.Contains(item.NegocioId)).ExecuteDeleteAsync(cancellationToken);
        await _context.Negocios.Where(item => negocioIds.Contains(item.Id)).ExecuteDeleteAsync(cancellationToken);
        await _context.Clientes.Where(item => clienteIds.Contains(item.Id) &&
            !_context.ClientesNegocios.Any(relacion => relacion.ClienteId == item.Id) &&
            !_context.Citas.Any(cita => cita.ClienteId == item.Id) &&
            !_context.Resenas.Any(resena => resena.ClienteId == item.Id)).ExecuteDeleteAsync(cancellationToken);
        await _context.TokensActualizacion.Where(item => item.UsuarioId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.UsuariosRoles.Where(item => item.UsuarioId == id).ExecuteDeleteAsync(cancellationToken);
        await _context.Usuarios.Where(item => item.Id == id).ExecuteDeleteAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task OlvidePasswordAsync(
        OlvidePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorCorreoAsync(NormalizarCorreo(request.Correo), cancellationToken);
        if (usuario is null || usuario.Estado != "active")
        {
            // No revelamos si el correo existe o no.
            return;
        }

        var token = _tokenService.CrearTokenRestablecimiento(usuario);
        await _emailSender.EnviarRestablecimientoPasswordAsync(usuario.Correo, token, cancellationToken);
    }

    public async Task<MaintenanceResult<bool>> RestablecerPasswordAsync(
        RestablecerPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var principal = _tokenService.ValidarToken(request.Token);
        if (principal is null || principal.FindFirst("purpose")?.Value != TokenService.PropositoRestablecimiento)
        {
            return MaintenanceResult<bool>.Fail(MaintenanceStatus.Invalid, "El enlace no es válido o expiró.");
        }

        if (!long.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var usuarioId))
        {
            return MaintenanceResult<bool>.Fail(MaintenanceStatus.Invalid, "El enlace no es válido.");
        }

        var usuario = await _usuarios.ObtenerPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
        {
            return MaintenanceResult<bool>.Fail(MaintenanceStatus.Invalid, "El enlace no es válido.");
        }

        var sello = principal.FindFirst("pwd_stamp")?.Value;
        if (sello != TokenService.SelloContrasena(usuario.ContrasenaHash))
        {
            return MaintenanceResult<bool>.Fail(MaintenanceStatus.Invalid, "El enlace ya fue utilizado o tu contraseña cambió después de solicitarlo.");
        }

        if (!InputRules.IsPasswordStrong(request.NuevaPassword, usuario.Correo, usuario.Telefono, usuario.Nombre, usuario.Apellido))
            return MaintenanceResult<bool>.Fail(MaintenanceStatus.Invalid, "nuevaPassword", "WEAK_PASSWORD", InputRules.PasswordMessage);

        usuario.ContrasenaHash = _passwordHasher.HashPassword(usuario, request.NuevaPassword);
        usuario.ActualizadoEn = DateTime.UtcNow;
        await _usuarios.GuardarCambiosAsync(cancellationToken);
        await _sessions.RevocarTodasDelUsuarioAsync(usuario.Id, cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    public async Task<MaintenanceResult<SesionAutenticada>> IniciarSesionConGoogleAsync(
        GoogleLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var clientId = _configuration["Google:ClientId"];
            var settings = new GoogleJsonWebSignature.ValidationSettings();
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                settings.Audience = new[] { clientId };
            }

            payload = await GoogleJsonWebSignature.ValidateAsync(request.CredentialToken, settings);
        }
        catch (InvalidJwtException)
        {
            return MaintenanceResult<SesionAutenticada>.Fail(MaintenanceStatus.Invalid, "No pudimos verificar tu cuenta de Google.");
        }

        // No se crea cuenta nueva: solo permite iniciar sesión a usuarios que ya pertenecen a un negocio.
        var usuario = await _usuarios.ObtenerPorCorreoAsync(NormalizarCorreo(payload.Email), cancellationToken);
        if (usuario is null || usuario.Estado != "active")
        {
            return MaintenanceResult<SesionAutenticada>.Fail(MaintenanceStatus.Forbidden, "No existe una cuenta con este correo. Pídele acceso al dueño de tu negocio.");
        }

        usuario.UltimoLoginEn = DateTime.UtcNow;
        await _usuarios.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<SesionAutenticada>.Ok(await _sessions.CrearAsync(usuario, request.RecordarSesion, cancellationToken));
    }

    public Task<SesionAutenticada?> RefrescarSesionAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        _sessions.RefrescarAsync(refreshToken, cancellationToken);

    public Task CerrarSesionAsync(string? refreshToken, CancellationToken cancellationToken = default) =>
        _sessions.RevocarAsync(refreshToken, cancellationToken);

    private static UsuarioResponse Mapear(Usuario usuario) => new(
        usuario.Id,
        usuario.Nombre,
        usuario.Apellido,
        usuario.Correo,
        usuario.Estado == "active",
        usuario.CreadoEn);

    private static string NormalizarCorreo(string correo) => InputNormalizer.NormalizeEmail(correo);
}
