using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class Negocio
{
    public long Id { get; set; }

    public long UsuarioPropietarioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string TipoNegocio { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? Rnc { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public string? LogoUrl { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual ICollection<CategoriasServicio> CategoriasServicios { get; set; } = new List<CategoriasServicio>();

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<ClientesNegocio> ClientesNegocios { get; set; } = new List<ClientesNegocio>();

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    public virtual ICollection<FeriadoNegocio> FeriadosNegocios { get; set; } = new List<FeriadoNegocio>();

    public virtual ICollection<MiembrosNegocio> MiembrosNegocios { get; set; } = new List<MiembrosNegocio>();

    public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();

    public virtual ICollection<RegistroAuditoria> RegistrosAuditoria { get; set; } = new List<RegistroAuditoria>();

    public virtual ICollection<Resena> Resenas { get; set; } = new List<Resena>();

    public virtual ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();

    public virtual ICollection<Sucursal> Sucursales { get; set; } = new List<Sucursal>();

    public virtual ICollection<SuscripcionesNegocio> SuscripcionesNegocios { get; set; } = new List<SuscripcionesNegocio>();

    public virtual Usuario UsuarioPropietario { get; set; } = null!;
}
