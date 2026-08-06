using System.ComponentModel.DataAnnotations;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.DTOs.Negocios;

public sealed class ActualizarNegocioRequest
{
    [Required, MaxLength(150), CommercialText] public string Nombre { get; set; } = null!;
    [MaxLength(30)] public string? Rnc { get; set; }
    [MaxLength(30), ValidPhone] public string? Telefono { get; set; }
    [MaxLength(150), RealisticEmail] public string? Correo { get; set; }
    [MaxLength(2000)] public string? Descripcion { get; set; }
    [MaxLength(500), Url] public string? LogoUrl { get; set; }
    [Required] public ActualizarSucursalPrincipalRequest SucursalPrincipal { get; set; } = null!;
    [Required, MinLength(7)] public List<ActualizarHorarioNegocioRequest> Horarios { get; set; } = [];
    public List<ActualizarFeriadoNegocioRequest> Feriados { get; set; } = [];
}

public sealed class ActualizarFeriadoNegocioRequest
{
    public DateOnly Fecha { get; set; }
    [Required, MaxLength(150), CommercialText] public string Nombre { get; set; } = null!;
}

public sealed class ActualizarSucursalPrincipalRequest
{
    [Required, MaxLength(150), CommercialText] public string Nombre { get; set; } = null!;
    [MaxLength(30), ValidPhone] public string? Telefono { get; set; }
    [Required, MaxLength(250), CommercialText] public string Direccion { get; set; } = null!;
    [Required, MaxLength(100)] public string Ciudad { get; set; } = null!;
    [Required, MaxLength(100)] public string Provincia { get; set; } = null!;
    [Required, MaxLength(100)] public string Pais { get; set; } = null!;
}

public sealed class ActualizarHorarioNegocioRequest
{
    [Range(0, 6)] public short DiaSemana { get; set; }
    public TimeOnly? AbreA { get; set; }
    public TimeOnly? CierraA { get; set; }
    public bool Cerrado { get; set; }
}
