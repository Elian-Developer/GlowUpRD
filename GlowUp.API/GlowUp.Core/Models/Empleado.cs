using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class Empleado
{
    public long Id { get; set; }

    public long NegocioId { get; set; }

    public long? SucursalId { get; set; }

    public long? UsuarioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public string? Puesto { get; set; }

    public string? Biografia { get; set; }

    public string? FotoUrl { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    // Conserva el historial de citas y ausencias, pero excluye al empleado de
    // los directorios y catálogos operativos.
    public DateTime? EliminadoEn { get; set; }

    public virtual ICollection<AusenciasEmpleado> AusenciasEmpleados { get; set; } = new List<AusenciasEmpleado>();

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<EmpleadoSucursal> EmpleadosSucursales { get; set; } = new List<EmpleadoSucursal>();

    public virtual ICollection<HorariosEmpleado> HorariosEmpleados { get; set; } = new List<HorariosEmpleado>();

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual ICollection<ServiciosEmpleado> ServiciosEmpleados { get; set; } = new List<ServiciosEmpleado>();

    public virtual Sucursal? Sucursal { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
