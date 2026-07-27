using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class ClientesNegocio
{
    public long Id { get; set; }

    public long NegocioId { get; set; }

    public long ClienteId { get; set; }

    public string? NotasInternas { get; set; }

    public DateTime? PrimeraVisitaEn { get; set; }

    public DateTime? UltimaVisitaEn { get; set; }

    public int TotalVisitas { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual Negocio Negocio { get; set; } = null!;
}
