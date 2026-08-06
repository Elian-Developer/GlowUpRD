using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class Sucursal
{
    public long Id { get; set; }

    public long NegocioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Telefono { get; set; }

    public string Direccion { get; set; } = null!;

    public string Ciudad { get; set; } = null!;

    public string Provincia { get; set; } = null!;

    public string Pais { get; set; } = null!;

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public bool EsPrincipal { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    public virtual ICollection<EmpleadoSucursal> EmpleadosSucursales { get; set; } = new List<EmpleadoSucursal>();

    public virtual ICollection<FeriadoNegocio> FeriadosNegocios { get; set; } = new List<FeriadoNegocio>();

    public virtual ICollection<HorariosEmpleado> HorariosEmpleados { get; set; } = new List<HorariosEmpleado>();

    public virtual ICollection<HorariosNegocio> HorariosNegocios { get; set; } = new List<HorariosNegocio>();

    public virtual ICollection<MiembrosNegocio> MiembrosNegocios { get; set; } = new List<MiembrosNegocio>();

    public virtual Negocio Negocio { get; set; } = null!;
}
