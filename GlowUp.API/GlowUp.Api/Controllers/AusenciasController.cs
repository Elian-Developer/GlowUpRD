using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GlowUpRD.API.DTOs.Ausencias;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GlowUpRD.API.Extensions;

namespace GlowUpRD.API.Controllers;

[Authorize, ApiController, Route("api/ausencias")]
public sealed class AusenciasController : ControllerBase
{
    private readonly IAusenciaService _service;
    public AusenciasController(IAusenciaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AusenciaResponse>>> Buscar([FromQuery] long negocioId, [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, [FromQuery] long? empleadoId, [FromQuery] bool incluirCanceladas, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.BuscarAsync(userId, negocioId, desde, hasta, empleadoId, incluirCanceladas, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AusenciaResponse>> Crear([FromBody] GuardarAusenciaRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.CrearAsync(userId, request, cancellationToken);
        return result.Status == MaintenanceStatus.Success ? StatusCode(201, result.Data) : Convert(result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<AusenciaResponse>> Actualizar(long id, [FromBody] GuardarAusenciaRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ActualizarAsync(userId, id, request, cancellationToken));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Cancelar(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.CancelarAsync(userId, id, cancellationToken);
        return result.Status == MaintenanceStatus.Success ? NoContent() : Convert(result).Result!;
    }

    private bool TryGetUserId(out long id) => long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub), out id);
    private ActionResult<T> Convert<T>(MaintenanceResult<T> result) => this.ToApiResult(result);
}
