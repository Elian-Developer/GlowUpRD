using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class PlanesSuscripcion
{
    public long Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public decimal PrecioMensual { get; set; }

    public int MaxSucursales { get; set; }

    public int MaxEmpleados { get; set; }

    public int MaxServicios { get; set; }

    public bool PermiteReportes { get; set; }

    public bool PermiteNotificaciones { get; set; }

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual ICollection<SuscripcionesNegocio> SuscripcionesNegocios { get; set; } = new List<SuscripcionesNegocio>();
}
