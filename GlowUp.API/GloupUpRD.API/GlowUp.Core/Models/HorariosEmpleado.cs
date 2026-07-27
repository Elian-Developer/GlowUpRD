using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class HorariosEmpleado
{
    public long Id { get; set; }

    public long EmpleadoId { get; set; }

    public short DiaSemana { get; set; }

    public TimeOnly IniciaA { get; set; }

    public TimeOnly TerminaA { get; set; }

    public bool Activo { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;
}
