using GloupUpRD.API.DTOs.Autenticacion;
using GloupUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloupUpRD.API.Controllers;

[Route("api/autenticacion")]
[ApiController]
public class AutenticacionController : ControllerBase
{
    private readonly IAuthService _authService;

    public AutenticacionController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("iniciar-sesion")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> IniciarSesion(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _authService.IniciarSesionAsync(request, cancellationToken);

        return resultado is null
            ? Unauthorized(new ProblemDetails { Title = "Correo o contraseña incorrectos." })
            : Ok(resultado);
    }

    [Authorize]
    [HttpGet("usuarios/{id:long}")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioResponse>> ObtenerPorId(
        long id,
        CancellationToken cancellationToken)
    {
        var usuario = await _authService.ObtenerPorIdAsync(id, cancellationToken);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [Authorize]
    [HttpPut("usuarios/{id:long}")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> Actualizar(
        long id,
        [FromBody] ActualizarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _authService.ActualizarAsync(id, request, cancellationToken);

        return resultado.Estado switch
        {
            ActualizarUsuarioEstado.NoEncontrado => NotFound(),
            ActualizarUsuarioEstado.CorreoDuplicado => Conflict(new ProblemDetails
            {
                Title = "El correo ya pertenece a otro usuario."
            }),
            _ => Ok(resultado.Usuario)
        };
    }

    [Authorize]
    [HttpDelete("usuarios/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(
        long id,
        CancellationToken cancellationToken)
    {
        var desactivado = await _authService.DesactivarAsync(id, cancellationToken);
        return desactivado ? NoContent() : NotFound();
    }

    [AllowAnonymous]
    [HttpPost("olvide-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> OlvidePassword(
        [FromBody] OlvidePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.OlvidePasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("restablecer-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RestablecerPassword(
        [FromBody] RestablecerPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _authService.RestablecerPasswordAsync(request, cancellationToken);
        return resultado.Status == MaintenanceStatus.Success
            ? NoContent()
            : BadRequest(new ProblemDetails { Title = resultado.Error });
    }

    [AllowAnonymous]
    [HttpPost("google")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginResponse>> Google(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _authService.IniciarSesionConGoogleAsync(request, cancellationToken);
        return resultado.Status switch
        {
            MaintenanceStatus.Success => Ok(resultado.Data),
            MaintenanceStatus.Forbidden => StatusCode(403, new ProblemDetails { Title = resultado.Error }),
            _ => BadRequest(new ProblemDetails { Title = resultado.Error })
        };
    }
}
