using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.Models;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GlowUpRD.API.Services.Implementations;

public sealed class TokenService : ITokenService
{
    public const string PropositoRestablecimiento = "password_reset";

    private readonly IConfiguration _configuration;
    public TokenService(IConfiguration configuration) => _configuration = configuration;

    public LoginResponse CrearToken(Usuario usuario)
    {
        var expirationMinutes = _configuration.GetValue("Jwt:ExpirationMinutes", 60);
        var expiration = DateTime.UtcNow.AddMinutes(expirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Correo),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        return new LoginResponse(
            EscribirToken(claims, expiration),
            expiration,
            Mapear(usuario));
    }

    public string CrearTokenRestablecimiento(Usuario usuario)
    {
        var expiration = DateTime.UtcNow.AddMinutes(15);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim("purpose", PropositoRestablecimiento),
            new Claim("pwd_stamp", SelloContrasena(usuario.ContrasenaHash)),
        };

        return EscribirToken(claims, expiration);
    }

    public ClaimsPrincipal? ValidarToken(string token)
    {
        // Evita que "sub" y otros claims cortos se remapeen a URIs largas (comportamiento
        // por defecto de JwtSecurityTokenHandler), para poder leerlos con su nombre original.
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        try
        {
            return handler.ValidateToken(token, ParametrosValidacion(), out _);
        }
        catch
        {
            return null;
        }
    }

    public static string SelloContrasena(string contrasenaHash)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contrasenaHash));
        return Convert.ToHexString(bytes)[..16];
    }

    private string EscribirToken(IEnumerable<Claim> claims, DateTime expiration)
    {
        var key = _configuration["Jwt:Key"]!;
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiration,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private TokenValidationParameters ParametrosValidacion()
    {
        var key = _configuration["Jwt:Key"]!;
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.Zero,
        };
    }

    private static UsuarioResponse Mapear(Usuario usuario) => new(
        usuario.Id,
        usuario.Nombre,
        usuario.Apellido,
        usuario.Correo,
        usuario.Estado == "active",
        usuario.CreadoEn);
}
