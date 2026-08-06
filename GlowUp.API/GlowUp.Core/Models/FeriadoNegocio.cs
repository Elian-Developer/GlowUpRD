namespace GlowUpRD.API.Models;

public partial class FeriadoNegocio
{
    public long Id { get; set; }
    public long NegocioId { get; set; }
    public long SucursalId { get; set; }
    public DateOnly Fecha { get; set; }
    public string Nombre { get; set; } = null!;
    public DateTime CreadoEn { get; set; }
    public virtual Negocio Negocio { get; set; } = null!;
    public virtual Sucursal Sucursal { get; set; } = null!;
}
