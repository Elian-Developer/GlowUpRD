using System;
using System.Collections.Generic;

namespace GlowUpRD.API.Models;

public partial class Cliente
{
    public long Id { get; set; }

    public long? UsuarioId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Apellido { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public string? Genero { get; set; }

    public string? Notas { get; set; }

    public DateTime CreadoEn { get; set; }

    public DateTime? ActualizadoEn { get; set; }

    public virtual ICollection<Cita> Cita { get; set; } = new List<Cita>();

    public virtual ICollection<ClientesNegocio> ClientesNegocios { get; set; } = new List<ClientesNegocio>();

    public virtual ICollection<Resena> Resenas { get; set; } = new List<Resena>();

    public virtual Usuario? Usuario { get; set; }
}
