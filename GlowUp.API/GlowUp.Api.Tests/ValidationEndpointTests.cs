using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace GlowUpRD.API.Tests;

public sealed class ValidationEndpointTests : IClassFixture<ValidationApiFactory>
{
    private readonly HttpClient _client;
    public ValidationEndpointTests(ValidationApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Register_InvalidBody_ReturnsUniformFieldErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/negocios", new { });
        var body = await response.Content.ReadFromJsonAsync<ApiErrorBody>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.NotEmpty(body.Errors);
        Assert.Contains(body.Errors, error => error.Field == "nombre");
        Assert.All(body.Errors, error => Assert.False(string.IsNullOrWhiteSpace(error.Message)));
    }

    [Fact]
    public async Task ProtectedServiceEndpoint_InvalidBody_ReturnsFieldErrorsBeforeDatabaseAccess()
    {
        using var client = _client.WithUser(1);
        var response = await client.PostAsJsonAsync("/api/servicios", new
        {
            negocioId = 0,
            nombre = "<<<>>>",
            precio = 0,
            duracionMinutos = 0,
            minutosAntes = -1
        });
        var body = await response.Content.ReadFromJsonAsync<ApiErrorBody>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(body!.Errors, error => error.Field == "negocioId");
        Assert.Contains(body.Errors, error => error.Field == "nombre");
        Assert.Contains(body.Errors, error => error.Field == "precio");
        Assert.Contains(body.Errors, error => error.Field == "duracionMinutos");
        Assert.Contains(body.Errors, error => error.Field == "minutosAntes");
    }

    [Fact]
    public async Task ProtectedAbsenceEndpoint_InvalidType_ReturnsFieldError()
    {
        using var client = _client.WithUser(1);
        var response = await client.PostAsJsonAsync("/api/ausencias", new
        {
            negocioId = 1,
            empleadoId = 1,
            iniciaEn = DateTime.Today.AddDays(1).AddHours(9),
            terminaEn = DateTime.Today.AddDays(1).AddHours(10),
            tipo = "medical-leave"
        });
        var body = await response.Content.ReadFromJsonAsync<ApiErrorBody>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(body!.Errors, error => error.Field == "tipo");
    }

    [Fact]
    public async Task UserProfile_OtherUser_ReturnsForbiddenWithoutDatabaseAccess()
    {
        using var client = _client.WithUser(1);
        var response = await client.GetAsync("/api/autenticacion/usuarios/2");
        var body = await response.Content.ReadFromJsonAsync<ApiErrorBody>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotNull(body);
        var error = Assert.Single(body!.Errors);
        Assert.Equal("id", error.Field);
        Assert.Equal("FORBIDDEN", error.Code);
    }

    [Fact]
    public async Task RefreshWithoutCookie_ReturnsSafeSessionExpiredError()
    {
        var response = await _client.PostAsync("/api/autenticacion/refrescar", content: null);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorBody>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(body!.Errors, error => error.Code == "SESSION_EXPIRED");
    }

    private sealed record ApiErrorBody(bool Success, string Message, List<ApiFieldErrorBody> Errors);
    private sealed record ApiFieldErrorBody(string Field, string Code, string Message);
}

public sealed class ValidationApiFactory : WebApplicationFactory<Program>
{
    public ValidationApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=localhost;Port=5432;Database=glowup_test;Username=test;Password=test");
        Environment.SetEnvironmentVariable("Jwt__Key", "testing-key-with-at-least-thirty-two-characters");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "GlowUpRD.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "GlowUpRD.Tests");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=glowup_test;Username=test;Password=test",
            ["Jwt:Key"] = "testing-key-with-at-least-thirty-two-characters",
            ["Jwt:Issuer"] = "GlowUpRD.Tests",
            ["Jwt:Audience"] = "GlowUpRD.Tests",
            ["Cors:AllowedOrigins:0"] = "http://localhost"
        }));
    }
}

internal static class TestClientExtensions
{
    private const string JwtKey = "testing-key-with-at-least-thirty-two-characters";

    public static HttpClient WithUser(this HttpClient client, long userId)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "GlowUpRD.Tests",
            audience: "GlowUpRD.Tests",
            claims: [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }
}
