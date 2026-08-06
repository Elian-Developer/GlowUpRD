using GlowUpRD.API.DTOs.Autenticacion;

namespace GlowUpRD.API.Extensions;

public static class RefreshCookieExtensions
{
    public const string RefreshCookieName = "glowup_refresh";

    public static void SetRefreshCookie(this HttpResponse response, SesionAutenticada session, IHostEnvironment environment) =>
        response.Cookies.Append(RefreshCookieName, session.RefreshToken, CookieOptions(session.RefreshExpiraEnUtc, session.Persistir, environment));

    public static void ClearRefreshCookie(this HttpResponse response, IHostEnvironment environment) =>
        response.Cookies.Delete(RefreshCookieName, CookieOptions(DateTime.UtcNow.AddDays(-1), true, environment));

    private static CookieOptions CookieOptions(DateTime expires, bool persistir, IHostEnvironment environment) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = "/api/autenticacion",
        Expires = persistir ? new DateTimeOffset(DateTime.SpecifyKind(expires, DateTimeKind.Utc)) : null,
    };
}
