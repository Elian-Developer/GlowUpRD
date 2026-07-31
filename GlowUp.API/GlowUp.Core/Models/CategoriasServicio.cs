using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class CategoriasServicio
{
    public long Id { get; set; }

    public long NegocioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int Orden { get; set; }

    public bool Activo { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
}
