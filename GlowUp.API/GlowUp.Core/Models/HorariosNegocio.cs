using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class HorariosNegocio
{
    public long Id { get; set; }

    public long SucursalId { get; set; }

    public short DiaSemana { get; set; }

    public TimeOnly? AbreA { get; set; }

    public TimeOnly? CierraA { get; set; }

    public bool Cerrado { get; set; }

    public virtual Sucursal Sucursal { get; set; } = null!;
}
