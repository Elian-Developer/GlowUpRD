using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class SuscripcionesNegocio
{
    public long Id { get; set; }

    public long NegocioId { get; set; }

    public long PlanId { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime IniciadaEn { get; set; }

    public DateTime? FinalizaEn { get; set; }

    public DateTime? ProximoCobroEn { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual PlanesSuscripcion Plan { get; set; } = null!;
}
