using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Resena
{
    public long Id { get; set; }

    public long CitaId { get; set; }

    public long ClienteId { get; set; }

    public long NegocioId { get; set; }

    public short Calificacion { get; set; }

    public string? Comentario { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Cita Cita { get; set; } = null!;

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual Negocio Negocio { get; set; } = null!;
}
