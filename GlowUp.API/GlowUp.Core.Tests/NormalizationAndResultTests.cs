using GlowUpRD.API.Services.Interfaces;
using GlowUpRD.API.Validation;
using Xunit;

namespace GlowUpRD.API.Core.Tests;

public sealed class NormalizationAndResultTests
{
    [Theory]
    [InlineData("  Mar\u00eda   Jos\u00e9  ", "Mar\u00eda Jos\u00e9")]
    [InlineData("\tAv.   Winston  Churchill\n", "Av. Winston Churchill")]
    public void RequiredText_TrimsAndCollapsesWhitespace(string input, string expected)
    {
        Assert.Equal(expected, InputNormalizer.RequiredText(input));
    }

    [Fact]
    public void OptionalText_ConvertsWhitespaceOnlyToNull()
    {
        Assert.Null(InputNormalizer.OptionalText("   \t "));
    }

    [Theory]
    [InlineData("  ANA@DOMINIO.COM ", "ana@dominio.com")]
    [InlineData("persona+ventas@ejemplo.do", "persona+ventas@ejemplo.do")]
    public void NormalizeEmail_UsesOneCanonicalRepresentation(string input, string expected)
    {
        Assert.Equal(expected, InputNormalizer.NormalizeEmail(input));
    }

    [Theory]
    [InlineData("8095551234", "+18095551234")]
    [InlineData("(829) 555-1234", "+18295551234")]
    [InlineData("+34 612 34 56 78", "+34612345678")]
    public void NormalizePhone_ReturnsCanonicalE164(string input, string expected)
    {
        Assert.Equal(expected, InputNormalizer.NormalizePhone(input));
    }

    [Fact]
    public void MaintenanceResult_FailureCarriesStructuredFieldError()
    {
        var result = MaintenanceResult<bool>.Fail(
            MaintenanceStatus.Conflict,
            "correo",
            "DUPLICATE_EMAIL",
            "Ya existe una cuenta registrada con este correo.");

        Assert.Equal(MaintenanceStatus.Conflict, result.Status);
        var error = Assert.Single(result.Errors!);
        Assert.Equal("correo", error.Field);
        Assert.Equal("DUPLICATE_EMAIL", error.Code);
    }
}
