using System.ComponentModel.DataAnnotations;

namespace GloupUpRD.API.DTOs.Servicios;

public sealed class GuardarServicioRequest
{
    [Range(1, ulong.MaxValue)] public ulong NegocioId { get; set; }
    public ulong? CategoriaId { get; set; }
    [Required, MaxLength(150)] public string Nombre { get; set; } = null!;
    [MaxLength(2000)] public string? Descripcion { get; set; }
    [Range(1, 1440)] public uint DuracionMinutos { get; set; }
    [Range(typeof(decimal), "0", "99999999")] public decimal Precio { get; set; }
    [Range(0, 1440)] public uint MinutosAntes { get; set; }
    [Range(0, 1440)] public uint MinutosDespues { get; set; }
    public bool Activo { get; set; } = true;
}
