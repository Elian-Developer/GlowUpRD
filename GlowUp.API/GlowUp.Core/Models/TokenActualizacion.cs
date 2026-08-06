using System;

namespace GlowUpRD.API.Models;

public partial class TokenActualizacion
{
    public long Id { get; set; }
    public long UsuarioId { get; set; }
    public string TokenHash { get; set; } = null!;
    public Guid FamiliaId { get; set; }
    public DateTime ExpiraEn { get; set; }
    public DateTime CreadoEn { get; set; }
    public DateTime? RevocadoEn { get; set; }
    public bool Persistente { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;
}
