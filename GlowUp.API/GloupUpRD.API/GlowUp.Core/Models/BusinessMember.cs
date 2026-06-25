using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class BusinessMember
{
    public ulong Id { get; set; }

    public ulong BusinessId { get; set; }

    public ulong UserId { get; set; }

    public ulong? BranchId { get; set; }

    public string MemberRole { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Branch? Branch { get; set; }

    public virtual Business Business { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
