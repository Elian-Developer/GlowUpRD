using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Notification
{
    public ulong Id { get; set; }

    public ulong? UserId { get; set; }

    public ulong? BusinessId { get; set; }

    public ulong? AppointmentId { get; set; }

    public string Channel { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Appointment? Appointment { get; set; }

    public virtual Business? Business { get; set; }

    public virtual User? User { get; set; }
}
