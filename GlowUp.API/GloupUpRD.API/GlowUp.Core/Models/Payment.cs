using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Payment
{
    public ulong Id { get; set; }

    public ulong AppointmentId { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? TransactionReference { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;
}
