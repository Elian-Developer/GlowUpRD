using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Servicio
{
    public long Id { get; set; }

    public long NegocioId { get; set; }

    public long? CategoriaId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int DuracionMinutos { get; set; }

    public decimal Precio { get; set; }

    public int BufferAntesMinutos { get; set; }

    public int BufferDespuesMinutos { get; set; }

    public bool Activo { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual CategoriasServicio? Categoria { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<ServicioCita> ServiciosCita { get; set; } = new List<ServicioCita>();

    public virtual ICollection<ServiciosEmpleado> ServiciosEmpleados { get; set; } = new List<ServiciosEmpleado>();
}
