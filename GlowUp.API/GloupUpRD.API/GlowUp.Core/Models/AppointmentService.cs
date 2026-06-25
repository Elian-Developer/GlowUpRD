using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class AppointmentService
{
    public ulong Id { get; set; }

    public ulong AppointmentId { get; set; }

    public ulong ServiceId { get; set; }

    public string ServiceName { get; set; } = null!;

    public uint DurationMinutes { get; set; }

    public decimal Price { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
