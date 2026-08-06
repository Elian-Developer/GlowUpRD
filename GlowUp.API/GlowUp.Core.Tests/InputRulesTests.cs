using GlowUpRD.API.Validation;
using Xunit;

namespace GlowUpRD.API.Core.Tests;

public sealed class InputRulesTests
{
    [Theory]
    [InlineData("María José")]
    [InlineData("Ana-María")]
    [InlineData("O'Connor")]
    public void PersonName_AcceptsValidNames(string value) => Assert.True(InputRules.IsPersonName(value));

    [Theory]
    [InlineData("Luis123")]
    [InlineData("María@")]
    [InlineData("4567")]
    [InlineData("--Carlos")]
    public void PersonName_RejectsInvalidNames(string value) => Assert.False(InputRules.IsPersonName(value));

    [Theory]
    [InlineData("8095551234", "+18095551234")]
    [InlineData("(809) 555-1234", "+18095551234")]
    [InlineData("+1 809 555 1234", "+18095551234")]
    [InlineData("+34612345678", "+34612345678")]
    public void Phone_NormalizesCanonicalFormat(string input, string expected) => Assert.Equal(expected, InputNormalizer.NormalizePhone(input));

    [Theory]
    [InlineData("name@domain.com")]
    [InlineData("nombre.apellido+turno@dominio.do")]
    public void Email_AcceptsRealisticFormat(string email) => Assert.True(InputRules.IsEmail(email));

    [Theory]
    [InlineData("name@domain")]
    [InlineData("name domain.com")]
    [InlineData("name@@domain.com")]
    public void Email_RejectsInvalidFormat(string email) => Assert.False(InputRules.IsEmail(email));

    [Fact]
    public void OptionalEmailAttribute_AcceptsOmittedValue()
    {
        var attribute = new RealisticEmailAttribute();
        Assert.True(attribute.IsValid(null));
        Assert.True(attribute.IsValid(""));
    }

    [Theory]
    [InlineData("SinNumero!")]
    [InlineData("sinmayuscula1!")]
    [InlineData("SINMINUSCULA1!")]
    [InlineData("SinSimbolo1")]
    public void Password_RejectsWeakValues(string password) => Assert.False(InputRules.IsPasswordStrong(password, "name@domain.com"));

    [Fact]
    public void Password_AcceptsStrongValue() => Assert.True(InputRules.IsPasswordStrong("ClaveSegura1!", "name@domain.com"));

    [Fact]
    public void BirthDate_RejectsFutureValue()
    {
        var today = new DateOnly(2026, 8, 5);
        Assert.False(InputRules.IsValidBirthDate(today.AddDays(1), today));
    }
}
