using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class ServiciosEmpleado
{
    public long EmpleadoId { get; set; }

    public long ServicioId { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;

    public virtual Servicio Servicio { get; set; } = null!;
}
