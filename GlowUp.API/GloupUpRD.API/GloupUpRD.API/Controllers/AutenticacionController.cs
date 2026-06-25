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
    [HttpPost("registrar")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> Registrar(
        [FromBody] RegistrarUsuarioRequest request,
        CancellationToken cancellationToken)
    {
        var resultado = await _authService.RegistrarAsync(request, cancellationToken);

        if (resultado is null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "El correo ya se encuentra registrado."
            });
        }

        return CreatedAtAction(nameof(ObtenerPorId), new { id = resultado.Id }, resultado);
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
        ulong id,
        CancellationToken cancellationToken)
    {
        var usuario = await _authService.ObtenerPorIdAsync(id, cancellationToken);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [Authorize]
    [HttpGet("usuarios")]
    [ProducesResponseType(typeof(IReadOnlyList<UsuarioResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UsuarioResponse>>> Buscar(
        [FromQuery] string? termino,
        CancellationToken cancellationToken) =>
        Ok(await _authService.BuscarAsync(termino, cancellationToken));

    [Authorize]
    [HttpPut("usuarios/{id:long}")]
    [ProducesResponseType(typeof(UsuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioResponse>> Actualizar(
        ulong id,
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
        ulong id,
        CancellationToken cancellationToken)
    {
        var desactivado = await _authService.DesactivarAsync(id, cancellationToken);
        return desactivado ? NoContent() : NotFound();
    }
}
