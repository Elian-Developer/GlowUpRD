using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Notificacion
{
    public long Id { get; set; }

    public long? UsuarioId { get; set; }

    public long? NegocioId { get; set; }

    public long? CitaId { get; set; }

    public string Canal { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public string Mensaje { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public DateTime? EnviadoEn { get; set; }

    public DateTime? LeidoEn { get; set; }

    public DateTime CreadoEn { get; set; }

    public virtual Cita? Cita { get; set; }

    public virtual Negocio? Negocio { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
