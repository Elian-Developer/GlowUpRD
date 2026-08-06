using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GlowUpRD.API.Validation;
using GlowUpRD.API.Extensions;

namespace GlowUpRD.API.Controllers;

[Route("api/autenticacion")]
[ApiController]
public class AutenticacionController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IHostEnvironment _environment;

    public AutenticacionController(IAuthService authService, IHostEnvironment environment)
    {
        _authService = authService;
        _environment = environment;
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

        if (resultado is null)
            return Unauthorized(ApiErrorResponse.From("No fue posible iniciar sesión.", new ApiFieldError("", "INVALID_CREDENTIALS", "Correo o contraseña incorrectos.")));

        Response.SetRefreshCookie(resultado, _environment);
        return Ok(resultado.Respuesta);
    }

    [Authorize]
    [HttpGet("usuarios/{id:long}")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioResponse>> ObtenerPorId(
        long id,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (userId != id) return StatusCode(StatusCodes.Status403Forbidden,
            ApiErrorResponse.From("No tienes permiso para realizar esta operación.", new ApiFieldError("id", "FORBIDDEN", "Solo puedes consultar tu propio perfil.")));
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
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (userId != id) return StatusCode(StatusCodes.Status403Forbidden,
            ApiErrorResponse.From("No tienes permiso para realizar esta operación.", new ApiFieldError("id", "FORBIDDEN", "Solo puedes actualizar tu propio perfil.")));
        var resultado = await _authService.ActualizarAsync(id, request, cancellationToken);

        return resultado.Estado switch
        {
            ActualizarUsuarioEstado.NoEncontrado => NotFound(ApiErrorResponse.From("El recurso solicitado no existe.", new ApiFieldError("id", "NOT_FOUND", "El usuario no existe."))),
            ActualizarUsuarioEstado.CorreoDuplicado => Conflict(ApiErrorResponse.From("No fue posible completar la operación.", new ApiFieldError("correo", "DUPLICATE_EMAIL", "Ya existe una cuenta registrada con este correo."))),
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
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (userId != id) return StatusCode(StatusCodes.Status403Forbidden,
            ApiErrorResponse.From("No tienes permiso para realizar esta operación.", new ApiFieldError("id", "FORBIDDEN", "Solo puedes desactivar tu propio perfil.")));
        var desactivado = await _authService.DesactivarAsync(id, cancellationToken);
        return desactivado ? NoContent() : NotFound(ApiErrorResponse.From("El recurso solicitado no existe.", new ApiFieldError("id", "NOT_FOUND", "El usuario no existe.")));
    }

    [Authorize]
    [HttpDelete("usuarios/{id:long}/cuenta")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarCuenta(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (userId != id) return StatusCode(StatusCodes.Status403Forbidden,
            ApiErrorResponse.From("No tienes permiso para realizar esta operación.", new ApiFieldError("id", "FORBIDDEN", "Solo puedes eliminar tu propia cuenta.")));

        var eliminada = await _authService.EliminarCuentaAsync(id, cancellationToken);
        if (!eliminada) return NotFound(ApiErrorResponse.From("El recurso solicitado no existe.", new ApiFieldError("id", "NOT_FOUND", "El usuario no existe.")));
        Response.ClearRefreshCookie(_environment);
        return NoContent();
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
            : this.ToApiError(resultado);
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
            MaintenanceStatus.Success => SetGoogleSession(resultado.Data!),
            MaintenanceStatus.Forbidden => this.ToApiError(resultado),
            _ => this.ToApiError(resultado)
        };
    }

    [AllowAnonymous]
    [HttpPost("refrescar")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Refrescar(CancellationToken cancellationToken)
    {
        var session = await _authService.RefrescarSesionAsync(Request.Cookies[RefreshCookieExtensions.RefreshCookieName] ?? string.Empty, cancellationToken);
        if (session is null)
        {
            Response.ClearRefreshCookie(_environment);
            return Unauthorized(ApiErrorResponse.From("La sesión expiró. Inicia sesión nuevamente.", new ApiFieldError("", "SESSION_EXPIRED", "La sesión expiró. Inicia sesión nuevamente.")));
        }

        Response.SetRefreshCookie(session, _environment);
        return Ok(session.Respuesta);
    }

    [AllowAnonymous]
    [HttpPost("cerrar-sesion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CerrarSesion(CancellationToken cancellationToken)
    {
        await _authService.CerrarSesionAsync(Request.Cookies[RefreshCookieExtensions.RefreshCookieName], cancellationToken);
        Response.ClearRefreshCookie(_environment);
        return NoContent();
    }

    private ActionResult<LoginResponse> SetGoogleSession(SesionAutenticada session)
    {
        Response.SetRefreshCookie(session, _environment);
        return Ok(session.Respuesta);
    }

    private bool TryGetUserId(out long id) => long.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub), out id);
}
