using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class EmployeeService
{
    public ulong EmployeeId { get; set; }

    public ulong ServiceId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
