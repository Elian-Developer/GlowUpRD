using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Role
{
    public long Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual ICollection<UsuariosRole> UsuariosRoles { get; set; } = new List<UsuariosRole>();
}
