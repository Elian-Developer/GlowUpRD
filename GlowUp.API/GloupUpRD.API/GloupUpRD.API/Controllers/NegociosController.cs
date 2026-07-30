using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GloupUpRD.API.DTOs.Autenticacion;
using GloupUpRD.API.DTOs.Negocios;
using GloupUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GloupUpRD.API.Controllers;

[ApiController, Route("api/negocios")]
public sealed class NegociosController : ControllerBase
{
    private readonly INegocioService _service;
    public NegociosController(INegocioService service) => _service = service;

    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> Registrar(RegistrarNegocioRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.RegistrarAsync(request, cancellationToken);
        return result.Status == MaintenanceStatus.Success ? Created(string.Empty, result.Data) : Convert(result);
    }

    [Authorize]
    [HttpGet("{id:long}")]
    public async Task<ActionResult<NegocioDetalleResponse>> Obtener(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ObtenerAsync(userId, id, cancellationToken));
    }

    [Authorize]
    [HttpPut("{id:long}")]
    public async Task<ActionResult<NegocioDetalleResponse>> Actualizar(long id, ActualizarNegocioRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ActualizarAsync(userId, id, request, cancellationToken));
    }

    [Authorize]
    [HttpGet("{id:long}/usuarios")]
    public async Task<ActionResult<IReadOnlyList<MiembroNegocioResponse>>> Miembros(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ObtenerMiembrosAsync(userId, id, cancellationToken));
    }

    [Authorize]
    [HttpPost("{id:long}/usuarios")]
    public async Task<ActionResult<MiembroNegocioResponse>> CrearUsuario(long id, CrearUsuarioNegocioRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.CrearUsuarioAsync(userId, id, request, cancellationToken);
        return result.Status == MaintenanceStatus.Success
            ? CreatedAtAction(nameof(Miembros), new { id }, result.Data)
            : Convert(result);
    }

    private bool TryGetUserId(out long id) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub), out id);
    private ActionResult<T> Convert<T>(MaintenanceResult<T> result) => result.Status switch
    {
        MaintenanceStatus.Success => Ok(result.Data),
        MaintenanceStatus.NotFound => NotFound(Problem(result.Error)),
        MaintenanceStatus.Forbidden => StatusCode(403, Problem(result.Error)),
        MaintenanceStatus.Conflict => Conflict(Problem(result.Error)),
        _ => BadRequest(Problem(result.Error))
    };
    private static ProblemDetails Problem(string? title) => new() { Title = title };
}
