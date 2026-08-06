using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GlowUpRD.API.DTOs.Citas;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GlowUpRD.API.Extensions;

namespace GlowUpRD.API.Controllers;

[Authorize, ApiController, Route("api/citas")]
public sealed class CitasController : ControllerBase
{
    private readonly ICitaService _service;
    public CitasController(ICitaService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CitaResponse>>> Buscar([FromQuery] long negocioId, [FromQuery] DateOnly? desde, [FromQuery] DateOnly? hasta, [FromQuery] long? sucursalId, [FromQuery] long? empleadoId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var start = desde ?? DateOnly.FromDateTime(DateTime.Today);
        return Convert(await _service.BuscarAsync(userId, negocioId, start, hasta ?? start, sucursalId, empleadoId, cancellationToken));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CitaResponse>> Obtener(long id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ObtenerAsync(userId, id, cancellationToken));
    }

    [HttpGet("catalogos")]
    public async Task<ActionResult<CatalogoCitasResponse>> Catalogos([FromQuery] long negocioId, [FromQuery] long? sucursalId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ObtenerCatalogosAsync(userId, negocioId, sucursalId, cancellationToken));
    }

    [HttpGet("negocios")]
    public async Task<ActionResult<IReadOnlyList<NegocioResumenResponse>>> Negocios(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await _service.ObtenerNegociosAsync(userId, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CitaResponse>> Crear(GuardarCitaRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await _service.CrearAsync(userId, request, cancellationToken);
        return result.Status == MaintenanceStatus.Success
            ? CreatedAtAction(nameof(Obtener), new { id = result.Data!.Id }, result.Data)
            : Convert(result);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CitaResponse>> Actualizar(long id, GuardarCitaRequest request, CancellationToken cancellationToken)
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
    private ActionResult<T> Convert<T>(MaintenanceResult<T> result) => this.ToApiResult(result);
}
