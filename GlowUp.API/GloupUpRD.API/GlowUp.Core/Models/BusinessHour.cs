using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class BusinessHour
{
    public ulong Id { get; set; }

    public ulong BranchId { get; set; }

    public byte DayOfWeek { get; set; }

    public TimeOnly? OpensAt { get; set; }

    public TimeOnly? ClosesAt { get; set; }

    public bool IsClosed { get; set; }

    public virtual Branch Branch { get; set; } = null!;
}
