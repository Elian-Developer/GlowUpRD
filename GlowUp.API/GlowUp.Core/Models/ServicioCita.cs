using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class ServicioCita
{
    public long Id { get; set; }

    public long CitaId { get; set; }

    public long ServicioId { get; set; }

    public string NombreServicio { get; set; } = null!;

    public int DuracionMinutos { get; set; }

    public decimal Precio { get; set; }

    public int BufferAntesMinutos { get; set; }

    public int BufferDespuesMinutos { get; set; }

    public virtual Cita Cita { get; set; } = null!;

    public virtual Servicio Servicio { get; set; } = null!;
}
