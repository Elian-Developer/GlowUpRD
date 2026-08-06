using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GlowUpRD.API.DTOs.Autenticacion;
using GlowUpRD.API.DTOs.Negocios;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GlowUpRD.API.Extensions;

namespace GlowUpRD.API.Controllers;

[ApiController, Route("api/negocios")]
public sealed class NegociosController : ControllerBase
{
    private readonly INegocioService _service;
    private readonly IHostEnvironment _environment;
    public NegociosController(INegocioService service, IHostEnvironment environment)
    {
        _service = service;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> Registrar(RegistrarNegocioRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.RegistrarAsync(request, cancellationToken);
        if (result.Status != MaintenanceStatus.Success) return this.ToApiError(result);
        Response.SetRefreshCookie(result.Data!, _environment);
        return Created(string.Empty, result.Data!.Respuesta);
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
    public async Task<ActionResult<NegocioDetalleResponse>> Actualizar(long id, [FromBody] ActualizarNegocioRequest request, CancellationToken cancellationToken)
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

    [Authorize]
    [HttpGet("{id:long}/sucursales")]
    public async Task<ActionResult<IReadOnlyList<SucursalResumenResponse>>> Sucursales(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ObtenerSucursalesAsync(userId, id, cancellationToken));
    }

    [Authorize]
    [HttpGet("{id:long}/sucursales/{sucursalId:long}")]
    public async Task<ActionResult<SucursalDetalleResponse>> ObtenerSucursal(long id, long sucursalId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ObtenerSucursalAsync(userId, id, sucursalId, cancellationToken));
    }

    [Authorize]
    [HttpPost("{id:long}/sucursales")]
    public async Task<ActionResult<SucursalDetalleResponse>> CrearSucursal(long id, GuardarSucursalRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.CrearSucursalAsync(userId, id, request, cancellationToken);
        return result.Status == MaintenanceStatus.Success
            ? CreatedAtAction(nameof(ObtenerSucursal), new { id, sucursalId = result.Data!.Id }, result.Data)
            : Convert(result);
    }

    [Authorize]
    [HttpPut("{id:long}/sucursales/{sucursalId:long}")]
    public async Task<ActionResult<SucursalDetalleResponse>> ActualizarSucursal(long id, long sucursalId, GuardarSucursalRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ActualizarSucursalAsync(userId, id, sucursalId, request, cancellationToken));
    }

    [Authorize]
    [HttpPost("{id:long}/sucursales/{sucursalId:long}/principal")]
    public async Task<ActionResult<SucursalDetalleResponse>> MarcarPrincipal(long id, long sucursalId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.MarcarSucursalPrincipalAsync(userId, id, sucursalId, cancellationToken));
    }

    [Authorize]
    [HttpDelete("{id:long}/sucursales/{sucursalId:long}")]
    public async Task<ActionResult> DesactivarSucursal(long id, long sucursalId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.DesactivarSucursalAsync(userId, id, sucursalId, cancellationToken);
        return result.Status == MaintenanceStatus.Success ? NoContent() : this.ToApiError(result);
    }

    [Authorize]
    [HttpPost("{id:long}/sucursales/{sucursalId:long}/reactivar")]
    public async Task<ActionResult<SucursalDetalleResponse>> ReactivarSucursal(long id, long sucursalId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ReactivarSucursalAsync(userId, id, sucursalId, cancellationToken));
    }

    private bool TryGetUserId(out long id) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub), out id);
    private ActionResult<T> Convert<T>(MaintenanceResult<T> result) => this.ToApiResult(result);
}
