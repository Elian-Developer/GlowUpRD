using System.Security.Claims;
using GloupUpRD.API.DTOs.Autenticacion;
using GloupUpRD.API.Models;

namespace GloupUpRD.API.Services.Interfaces;

public interface ITokenService
{
    LoginResponse CrearToken(Usuario usuario);
    string CrearTokenRestablecimiento(Usuario usuario);
    ClaimsPrincipal? ValidarToken(string token);
}
