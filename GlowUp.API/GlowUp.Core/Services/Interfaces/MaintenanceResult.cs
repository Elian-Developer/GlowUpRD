namespace GlowUpRD.API.Services.Interfaces;

public enum MaintenanceStatus { Success, NotFound, Forbidden, Conflict, Invalid }

public sealed record MaintenanceResult<T>(
    MaintenanceStatus Status,
    T? Data = default,
    string? Error = null,
    IReadOnlyList<GlowUpRD.API.Validation.ApiFieldError>? Errors = null)
{
    public static MaintenanceResult<T> Ok(T data) => new(MaintenanceStatus.Success, data);
    public static MaintenanceResult<T> Fail(MaintenanceStatus status, string error) =>
        new(status, default, error, [new("", "BUSINESS_RULE", error)]);
    public static MaintenanceResult<T> Fail(MaintenanceStatus status, string field, string code, string error) =>
        new(status, default, error, [new(field, code, error)]);
}
