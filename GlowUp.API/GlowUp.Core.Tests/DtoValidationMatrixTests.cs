using System.ComponentModel.DataAnnotations;
using GlowUpRD.API.DTOs.Ausencias;
using GlowUpRD.API.DTOs.Clientes;
using GlowUpRD.API.DTOs.Empleados;
using GlowUpRD.API.DTOs.Negocios;
using GlowUpRD.API.DTOs.Servicios;
using Xunit;

namespace GlowUpRD.API.Core.Tests;

public sealed class DtoValidationMatrixTests
{
    [Fact]
    public void EmployeeRequest_RejectsInvalidNameAndPhone()
    {
        var request = new GuardarEmpleadoRequest
        {
            NegocioId = 1,
            Nombre = "Ana7",
            Apellido = "P\u00e9rez",
            Telefono = "809ABC1234"
        };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.Nombre)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.Telefono)));
    }

    [Fact]
    public void ClientRequest_AcceptsAccentedHyphenatedAndApostropheNames()
    {
        var request = new GuardarClienteRequest
        {
            NegocioId = 1,
            Nombre = "Ana-Mar\u00eda",
            Apellido = "O'Connor",
            Correo = "ana@ejemplo.do",
            Telefono = "+18095551234"
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void ServiceRequest_RejectsExcessiveCurrencyPrecisionAndBuffersOutsideRange()
    {
        var request = new GuardarServicioRequest
        {
            NegocioId = 1,
            Nombre = "Color 360",
            Precio = 25.999m,
            DuracionMinutos = 30,
            MinutosAntes = -1,
            MinutosDespues = 1441
        };

        var errors = Validate(request);

        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.MinutosAntes)));
        Assert.Contains(errors, error => error.MemberNames.Contains(nameof(request.MinutosDespues)));
    }

    [Fact]
    public void BusinessProfile_RejectsUnsafeLogoUrl()
    {
        var request = new ActualizarNegocioRequest
        {
            Nombre = "Studio 24",
            LogoUrl = "javascript:alert(1)",
            SucursalPrincipal = new ActualizarSucursalPrincipalRequest
            {
                Nombre = "Principal",
                Direccion = "Av. Principal 1",
                Ciudad = "Santo Domingo",
                Provincia = "Distrito Nacional",
                Pais = "Rep\u00fablica Dominicana"
            },
            Horarios = Enumerable.Range(0, 7)
                .Select(day => new ActualizarHorarioNegocioRequest { DiaSemana = (short)day, Cerrado = true })
                .ToList()
        };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.LogoUrl)));
    }

    [Fact]
    public void BusinessRegistration_AcceptsOmittedOptionalBusinessEmail()
    {
        var request = new RegistrarNegocioRequest
        {
            Nombre = "Studio 24",
            TipoNegocio = "salon",
            Direccion = "Av. Principal 1",
            Ciudad = "Santo Domingo",
            Provincia = "Distrito Nacional",
            NombrePropietario = "Cesar",
            ApellidoPropietario = "Harguindeguy",
            CorreoPropietario = "cesarharguindeguy@gmail.com",
            Password = "ClaveSegura1!",
            ConfirmarPassword = "ClaveSegura1!"
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void AbsenceRequest_RejectsUnknownType()
    {
        var request = new GuardarAusenciaRequest
        {
            NegocioId = 1,
            EmpleadoId = 1,
            IniciaEn = DateTime.Today.AddDays(1),
            TerminaEn = DateTime.Today.AddDays(2),
            Tipo = "medical-leave"
        };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.Tipo)));
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
