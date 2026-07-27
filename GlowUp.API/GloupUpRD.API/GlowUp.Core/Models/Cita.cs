using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Cita
{
    public long Id { get; set; }

    public long NegocioId { get; set; }

    public long SucursalId { get; set; }

    public long ClienteId { get; set; }

    public long? ClienteNegocioId { get; set; }

    public long EmpleadoId { get; set; }

    public DateOnly FechaCita { get; set; }

    public DateTime Inicio { get; set; }

    public DateTime Fin { get; set; }

    public string Estado { get; set; } = null!;

    public string? MotivoCancelacion { get; set; }

    public string? Notas { get; set; }

    public decimal Total { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ClientesNegocio? ClienteNegocio { get; set; }

    public virtual Empleado Empleado { get; set; } = null!;

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();

    public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();

    public virtual Resena? Resena { get; set; }

    public virtual ICollection<ServicioCita> ServiciosCita { get; set; } = new List<ServicioCita>();

    public virtual Sucursal Sucursal { get; set; } = null!;
}
