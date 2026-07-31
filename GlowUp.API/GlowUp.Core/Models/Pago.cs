using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class Pago
{
    public long Id { get; set; }

    public long CitaId { get; set; }

    public decimal Monto { get; set; }

    public string Metodo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string? ReferenciaTransaccion { get; set; }

    public DateTime? PagadoEn { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Cita Cita { get; set; } = null!;
}
