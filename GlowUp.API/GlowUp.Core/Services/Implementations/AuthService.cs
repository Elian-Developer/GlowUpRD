using System.IdentityModel.Tokens.Jwt;
using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.Models;
using GlowUpRD.API.Repositories.Interfaces;
using GlowUpRD.API.Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace GlowUpRD.API.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUsuarioRepository usuarios,
        IPasswordHasher<Usuario> passwordHasher,
        ITokenService tokenService,
        IEmailSender emailSender,
        IConfiguration configuration)
    {
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public async Task<LoginResponse?> IniciarSesionAsync(
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

        return _tokenService.CrearToken(usuario);
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

        usuario.Nombre = request.Nombre.Trim();
        usuario.Apellido = request.Apellido.Trim();
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

        usuario.ContrasenaHash = _passwordHasher.HashPassword(usuario, request.NuevaPassword);
        usuario.ActualizadoEn = DateTime.UtcNow;
        await _usuarios.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<bool>.Ok(true);
    }

    public async Task<MaintenanceResult<LoginResponse>> IniciarSesionConGoogleAsync(
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
            return MaintenanceResult<LoginResponse>.Fail(MaintenanceStatus.Invalid, "No pudimos verificar tu cuenta de Google.");
        }

        // No se crea cuenta nueva: solo permite iniciar sesión a usuarios que ya pertenecen a un negocio.
        var usuario = await _usuarios.ObtenerPorCorreoAsync(NormalizarCorreo(payload.Email), cancellationToken);
        if (usuario is null || usuario.Estado != "active")
        {
            return MaintenanceResult<LoginResponse>.Fail(MaintenanceStatus.Forbidden, "No existe una cuenta con este correo. Pídele acceso al dueño de tu negocio.");
        }

        usuario.UltimoLoginEn = DateTime.UtcNow;
        await _usuarios.GuardarCambiosAsync(cancellationToken);
        return MaintenanceResult<LoginResponse>.Ok(_tokenService.CrearToken(usuario));
    }

    private static UsuarioResponse Mapear(Usuario usuario) => new(
        usuario.Id,
        usuario.Nombre,
        usuario.Apellido,
        usuario.Correo,
        usuario.Estado == "active",
        usuario.CreadoEn);

    private static string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();
}
