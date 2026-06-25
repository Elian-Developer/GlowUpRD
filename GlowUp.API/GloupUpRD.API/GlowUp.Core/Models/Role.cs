using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Role
{
    public ulong Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
