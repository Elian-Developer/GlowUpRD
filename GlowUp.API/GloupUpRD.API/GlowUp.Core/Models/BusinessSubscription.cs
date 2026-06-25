using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class BusinessSubscription
{
    public ulong Id { get; set; }

    public ulong BusinessId { get; set; }

    public ulong PlanId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime StartedAt { get; set; }

    public DateTime? EndsAt { get; set; }

    public DateTime? NextBillingAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Business Business { get; set; } = null!;

    public virtual SubscriptionPlan Plan { get; set; } = null!;
}
