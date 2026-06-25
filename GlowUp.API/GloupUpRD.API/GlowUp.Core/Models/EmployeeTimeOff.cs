using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class EmployeeTimeOff
{
    public ulong Id { get; set; }

    public ulong EmployeeId { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
