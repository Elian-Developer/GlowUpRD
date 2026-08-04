using System.ComponentModel.DataAnnotations;

namespace GlowUpRD.API.DTOs.Empleados;

public sealed class GuardarEmpleadoRequest
{
    [Range(1, long.MaxValue)] public long NegocioId { get; set; }
    public long? SucursalId { get; set; }
    [Required, MaxLength(100)] public string Nombre { get; set; } = null!;
    [Required, MaxLength(100)] public string Apellido { get; set; } = null!;
    [MaxLength(30)] public string? Telefono { get; set; }
    [EmailAddress, MaxLength(150)] public string? Correo { get; set; }
    [MaxLength(100)] public string? Puesto { get; set; }
    [MaxLength(2000)] public string? Biografia { get; set; }
    public bool Activo { get; set; } = true;
    public List<GuardarHorarioEmpleadoRequest> Horarios { get; set; } = [];

    public bool CrearAcceso { get; set; }
    [MinLength(8), MaxLength(100)] public string? Password { get; set; }
}

public sealed class GuardarHorarioEmpleadoRequest
{
    [Range(0, 6)] public short DiaSemana { get; set; }
    public TimeOnly? IniciaA { get; set; }
    public TimeOnly? TerminaA { get; set; }
    public bool Activo { get; set; }
}
