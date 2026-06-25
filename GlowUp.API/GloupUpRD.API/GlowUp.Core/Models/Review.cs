using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Review
{
    public ulong Id { get; set; }

    public ulong AppointmentId { get; set; }

    public ulong CustomerId { get; set; }

    public ulong BusinessId { get; set; }

    public byte Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Business Business { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
