using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class MiembrosNegocio
{
    public long Id { get; set; }

    public long NegocioId { get; set; }

    public long UsuarioId { get; set; }

    public long? SucursalId { get; set; }

    public string RolMiembro { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime CreadoEn { get; set; }

    public virtual Negocio Negocio { get; set; } = null!;

    public virtual Sucursal? Sucursal { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
