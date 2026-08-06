using GlowUpRD.API.Services.Interfaces;
using GlowUpRD.API.Validation;
using Microsoft.AspNetCore.Mvc;

namespace GlowUpRD.API.Extensions;

public static class ApiResponseExtensions
{
    public static ActionResult<T> ToApiResult<T>(this ControllerBase controller, MaintenanceResult<T> result) => result.Status switch
    {
        MaintenanceStatus.Success => controller.Ok(result.Data),
        MaintenanceStatus.NotFound => controller.NotFound(Error(result, "El recurso solicitado no existe.")),
        MaintenanceStatus.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, Error(result, "No tienes permiso para realizar esta operación.")),
        MaintenanceStatus.Conflict => controller.Conflict(Error(result, "No fue posible completar la operación.")),
        _ => controller.BadRequest(Error(result, "La información enviada contiene errores.")),
    };

    public static ObjectResult ToApiError<T>(this ControllerBase controller, MaintenanceResult<T> result) =>
        result.Status switch
        {
            MaintenanceStatus.NotFound => controller.NotFound(Error(result, "El recurso solicitado no existe.")),
            MaintenanceStatus.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, Error(result, "No tienes permiso para realizar esta operación.")),
            MaintenanceStatus.Conflict => controller.Conflict(Error(result, "No fue posible completar la operación.")),
            _ => controller.BadRequest(Error(result, "La información enviada contiene errores.")),
        };

    private static ApiErrorResponse Error<T>(MaintenanceResult<T> result, string fallback) =>
        new(false, result.Error ?? fallback, result.Errors ?? [new("", "BUSINESS_RULE", result.Error ?? fallback)]);
}
