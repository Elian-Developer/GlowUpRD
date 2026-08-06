using System;

namespace GlowUpRD.API.Models;

public partial class EmpleadoSucursal
{
    public long EmpleadoId { get; set; }
    public long SucursalId { get; set; }
    public string Estado { get; set; } = null!;
    public DateTime CreadoEn { get; set; }
    public virtual Empleado Empleado { get; set; } = null!;
    public virtual Sucursal Sucursal { get; set; } = null!;
}
