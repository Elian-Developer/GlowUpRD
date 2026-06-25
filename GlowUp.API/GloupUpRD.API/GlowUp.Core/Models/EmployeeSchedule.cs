using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class EmployeeSchedule
{
    public ulong Id { get; set; }

    public ulong EmployeeId { get; set; }

    public byte DayOfWeek { get; set; }

    public TimeOnly StartsAt { get; set; }

    public TimeOnly EndsAt { get; set; }

    public bool? IsActive { get; set; }

    public virtual Employee Employee { get; set; } = null!;
}
