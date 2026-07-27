using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class AusenciasEmpleado
{
    public long Id { get; set; }

    public long EmpleadoId { get; set; }

    public DateTime IniciaEn { get; set; }

    public DateTime TerminaEn { get; set; }

    public string? Motivo { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;
}
