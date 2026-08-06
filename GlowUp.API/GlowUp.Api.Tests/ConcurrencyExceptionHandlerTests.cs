using System.Text.Json;
using GlowUpRD.API.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GlowUpRD.API.Tests;

public sealed class ConcurrencyExceptionHandlerTests
{
    [Fact]
    public async Task OptimisticConcurrencyConflict_ReturnsStructured409()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new ApiExceptionHandler(NullLogger<ApiExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(context, new DbUpdateConcurrencyException(), CancellationToken.None);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ApiErrorBody>(context.Response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body!.Success);
        var error = Assert.Single(body.Errors);
        Assert.Equal("CONCURRENT_MODIFICATION", error.Code);
    }

    private sealed record ApiErrorBody(bool Success, string Message, List<ApiFieldErrorBody> Errors);
    private sealed record ApiFieldErrorBody(string Field, string Code, string Message);
}
