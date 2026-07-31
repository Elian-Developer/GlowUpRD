using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GlowUpRD.API.DTOs.Reportes;
using GlowUpRD.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GlowUpRD.API.Controllers;

[Authorize, ApiController, Route("api/reportes")]
public sealed class ReportesController : ControllerBase
{
    private readonly IReporteService _service;
    public ReportesController(IReporteService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<ReporteResponse>> Obtener([FromQuery] long negocioId, [FromQuery] DateOnly desde, [FromQuery] DateOnly hasta, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Convert(await _service.ObtenerAsync(userId, negocioId, desde, hasta, cancellationToken));
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
