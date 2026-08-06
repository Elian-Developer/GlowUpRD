using GlowUpRD.API.Validation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GlowUpRD.API.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, field, code, message) = exception switch
        {
            DbUpdateConcurrencyException =>
                (StatusCodes.Status409Conflict, "", "CONCURRENT_MODIFICATION", "Este registro fue modificado por otra persona. Recarga la información antes de guardar tus cambios."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg } =>
                (StatusCodes.Status409Conflict, FieldForConstraint(pg.ConstraintName), "DUPLICATE_VALUE", MessageForUniqueConstraint(pg.ConstraintName)),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                (StatusCodes.Status409Conflict, "", "RELATION_CONFLICT", "La información está relacionada con un registro que ya no está disponible."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.CheckViolation } } =>
                (StatusCodes.Status400BadRequest, "", "CONSTRAINT_VIOLATION", "La información enviada no cumple las reglas requeridas."),
            _ => (StatusCodes.Status500InternalServerError, "", "INTERNAL_ERROR", "No pudimos completar la operación. Inténtalo nuevamente.")
        };

        if (status >= 500) logger.LogError(exception, "Unhandled API exception for {Path}", context.Request.Path);
        else logger.LogWarning(exception, "Database integrity exception for {Path}", context.Request.Path);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            ApiErrorResponse.From(status == 409 ? "No fue posible completar la operación." : "La información enviada contiene errores.", new ApiFieldError(field, code, message)),
            cancellationToken);
        return true;
    }

    private static string FieldForConstraint(string? constraint) => constraint switch
    {
        "correo" or "usuarios_correo_normalizado_key" => "correo",
        "servicios_nombre_normalizado_key" => "nombre",
        _ => ""
    };

    private static string MessageForUniqueConstraint(string? constraint) => constraint switch
    {
        "correo" or "usuarios_correo_normalizado_key" => "Ya existe una cuenta registrada con este correo.",
        "servicios_nombre_normalizado_key" => "Ya existe un servicio con ese nombre en este negocio. Utiliza un nombre diferente.",
        _ => "Ya existe un registro con esa información."
    };
}
