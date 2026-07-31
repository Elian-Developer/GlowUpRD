using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class RegistroAuditoria
{
    public long Id { get; set; }

    public long? UsuarioId { get; set; }

    public long? NegocioId { get; set; }

    public string Accion { get; set; } = null!;

    public string EntidadNombre { get; set; } = null!;

    public long? EntidadId { get; set; }

    public string? ValoresAnteriores { get; set; }

    public string? ValoresNuevos { get; set; }

    public string? DireccionIp { get; set; }

    public string? AgenteUsuario { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Negocio? Negocio { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
