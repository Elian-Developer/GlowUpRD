using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class Usuario
{
    public long Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string? Telefono { get; set; }

    public string ContrasenaHash { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime? VerificadoEn { get; set; }

    public DateTime? UltimoLoginEn { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual ICollection<Cliente> Clientes { get; set; } = new List<Cliente>();

    public virtual ICollection<Empleado> Empleados { get; set; } = new List<Empleado>();

    public virtual ICollection<MiembrosNegocio> MiembrosNegocios { get; set; } = new List<MiembrosNegocio>();

    public virtual ICollection<Negocio> Negocios { get; set; } = new List<Negocio>();

    public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();

    public virtual ICollection<RegistroAuditoria> RegistrosAuditoria { get; set; } = new List<RegistroAuditoria>();

    public virtual ICollection<UsuariosRole> UsuariosRoles { get; set; } = new List<UsuariosRole>();
}
