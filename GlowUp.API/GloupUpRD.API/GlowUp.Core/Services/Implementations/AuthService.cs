using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GloupUpRD.API.DTOs.Autenticacion;
using GloupUpRD.API.Models;
using GloupUpRD.API.Repositories.Interfaces;
using GloupUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GloupUpRD.API.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUsuarioRepository usuarios,
        IPasswordHasher<Usuario> passwordHasher,
        IConfiguration configuration)
    {
        _usuarios = usuarios;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async Task<UsuarioResponse?> RegistrarAsync(
        RegistrarUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        var correo = NormalizarCorreo(request.Correo);
        if (await _usuarios.ObtenerPorCorreoAsync(correo, cancellationToken) is not null)
        {
            return null;
        }

        var usuario = new Usuario
        {
            Nombre = request.Nombre.Trim(),
            Apellido = request.Apellido.Trim(),
            Correo = correo,
            Estado = "active",
            CreadoEn = DateTime.UtcNow
        };

        usuario.ContrasenaHash = _passwordHasher.HashPassword(usuario, request.Password);
        await _usuarios.AgregarAsync(usuario, cancellationToken);
        await _usuarios.GuardarCambiosAsync(cancellationToken);

        return Mapear(usuario);
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

        return CrearToken(usuario);
    }

    public async Task<UsuarioResponse?> ObtenerPorIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(id, cancellationToken);
        return usuario is null ? null : Mapear(usuario);
    }

    public async Task<IReadOnlyList<UsuarioResponse>> BuscarAsync(
        string? termino,
        CancellationToken cancellationToken = default) =>
        (await _usuarios.BuscarAsync(termino, cancellationToken)).Select(Mapear).ToList();

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

    private LoginResponse CrearToken(Usuario usuario)
    {
        var key = _configuration["Jwt:Key"]!;
        var expirationMinutes = _configuration.GetValue("Jwt:ExpirationMinutes", 60);
        var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiration,
            Mapear(usuario));
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
