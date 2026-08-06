using System.ComponentModel.DataAnnotations;
using GlowUpRD.API.Validation;

namespace GlowUpRD.API.DTOs.Negocios;

public sealed class GuardarSucursalRequest
{
    [Required, MaxLength(150), CommercialText] public string Nombre { get; set; } = null!;
    [MaxLength(30), ValidPhone] public string? Telefono { get; set; }
    [Required, MaxLength(250), CommercialText] public string Direccion { get; set; } = null!;
    [Required, MaxLength(100)] public string Ciudad { get; set; } = null!;
    [Required, MaxLength(100)] public string Provincia { get; set; } = null!;
    [Required, MaxLength(100)] public string Pais { get; set; } = null!;
    public List<ActualizarHorarioNegocioRequest> Horarios { get; set; } = [];
    public List<ActualizarFeriadoNegocioRequest> Feriados { get; set; } = [];
    public bool AplicarFeriadosATodas { get; set; }
}

public sealed record SucursalResumenResponse(long Id, string Nombre, string Ciudad, bool EsPrincipal, string Estado);

public sealed record SucursalDetalleResponse(
    long Id, long NegocioId, string Nombre, string? Telefono, string Direccion,
    string Ciudad, string Provincia, string Pais, bool EsPrincipal, string Estado,
    IReadOnlyList<HorarioNegocioResponse> Horarios,
    IReadOnlyList<FeriadoNegocioResponse> Feriados);
