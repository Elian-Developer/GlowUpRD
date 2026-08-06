namespace GlowUpRD.API.Validation;

public sealed record ApiFieldError(string Field, string Code, string Message);

public sealed record ApiErrorResponse(
    bool Success,
    string Message,
    IReadOnlyList<ApiFieldError> Errors)
{
    public static ApiErrorResponse From(string message, params ApiFieldError[] errors) =>
        new(false, message, errors);
}
