using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GlowUpRD.API.DTOs.Clientes;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlowUpRD.API.Controllers;

[Authorize, ApiController, Route("api/clientes")]
public sealed class ClientesController : ControllerBase
{
    private readonly IClienteService _service;
    public ClientesController(IClienteService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClienteResponse>>> Buscar([FromQuery] long negocioId, [FromQuery] bool incluirInactivos, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.BuscarAsync(userId, negocioId, incluirInactivos, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ClienteResponse>> Obtener(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ObtenerAsync(userId, id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ClienteResponse>> Crear(GuardarClienteRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.CrearAsync(userId, request, cancellationToken);
        return result.Status == MaintenanceStatus.Success
            ? CreatedAtAction(nameof(Obtener), new { id = result.Data!.Id }, result.Data)
            : Convert(result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ClienteResponse>> Actualizar(long id, GuardarClienteRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ActualizarAsync(userId, id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Eliminar(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.EliminarAsync(userId, id, cancellationToken);
        return result.Status == MaintenanceStatus.Success ? NoContent() : Convert(result).Result!;
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
