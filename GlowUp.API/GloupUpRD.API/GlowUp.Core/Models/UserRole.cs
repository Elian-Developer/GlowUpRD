using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class UserRole
{
    public ulong UserId { get; set; }

    public ulong RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
