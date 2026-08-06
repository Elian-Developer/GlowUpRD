using System.ComponentModel.DataAnnotations;
using GlowUpRD.API.DTOs.Citas;
using GlowUpRD.API.DTOs.Clientes;
using GlowUpRD.API.DTOs.Servicios;
using Xunit;

namespace GlowUpRD.API.Core.Tests;

public sealed class RequestValidationTests
{
    [Fact]
    public void ClientRequest_RejectsNameWithNumbers()
    {
        var request = new GuardarClienteRequest { NegocioId = 1, Nombre = "Luis123", Apellido = "Pérez" };
        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.Nombre)));
    }

    [Fact]
    public void ClientRequest_RejectsInvalidEmail()
    {
        var request = new GuardarClienteRequest { NegocioId = 1, Nombre = "María", Apellido = "Pérez", Correo = "maria@dominio" };
        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.Correo)));
    }

    [Theory]
    [InlineData(-1, 30)]
    [InlineData(25, 0)]
    public void ServiceRequest_RejectsNegativePriceOrZeroDuration(decimal price, int duration)
    {
        var request = new GuardarServicioRequest { NegocioId = 1, Nombre = "Color 360", Precio = price, DuracionMinutos = duration };
        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void AppointmentRequest_RejectsZeroIdentifiers()
    {
        var request = new GuardarCitaRequest { Inicio = DateTime.Now.AddHours(1), ServicioIds = [1] };
        Assert.True(Validate(request).Count >= 4);
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
