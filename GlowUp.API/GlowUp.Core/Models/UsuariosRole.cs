using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class UsuariosRole
{
    public long UsuarioId { get; set; }

    public long RolId { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Role Rol { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;
}
