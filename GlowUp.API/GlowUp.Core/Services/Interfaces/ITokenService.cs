using System.Security.Claims;
using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.Models;

namespace GlowUpRD.API.Services.Interfaces;

public interface ITokenService
{
    LoginResponse CrearToken(Usuario usuario);
    string CrearTokenRestablecimiento(Usuario usuario);
    ClaimsPrincipal? ValidarToken(string token);
}
