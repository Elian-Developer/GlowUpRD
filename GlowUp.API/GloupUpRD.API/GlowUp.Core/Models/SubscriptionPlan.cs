using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class SubscriptionPlan
{
    public ulong Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal MonthlyPrice { get; set; }

    public uint MaxBranches { get; set; }

    public uint MaxEmployees { get; set; }

    public uint MaxServices { get; set; }

    public bool AllowsReports { get; set; }

    public bool? AllowsNotifications { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BusinessSubscription> BusinessSubscriptions { get; set; } = new List<BusinessSubscription>();
}
