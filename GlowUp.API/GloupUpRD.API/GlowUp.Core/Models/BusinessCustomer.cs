using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class BusinessCustomer
{
    public ulong Id { get; set; }

    public ulong BusinessId { get; set; }

    public ulong CustomerId { get; set; }

    public string? InternalNotes { get; set; }

    public DateTime? FirstVisitAt { get; set; }

    public DateTime? LastVisitAt { get; set; }

    public uint TotalVisits { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Business Business { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;
}
