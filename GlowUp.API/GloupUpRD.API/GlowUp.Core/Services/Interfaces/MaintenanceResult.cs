namespace GloupUpRD.API.Services.Interfaces;

public enum MaintenanceStatus { Success, NotFound, Forbidden, Conflict, Invalid }

public sealed record MaintenanceResult<T>(MaintenanceStatus Status, T? Data = default, string? Error = null)
{
    public static MaintenanceResult<T> Ok(T data) => new(MaintenanceStatus.Success, data);
    public static MaintenanceResult<T> Fail(MaintenanceStatus status, string error) => new(status, default, error);
}
