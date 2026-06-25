using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class ServiceCategory
{
    public ulong Id { get; set; }

    public ulong BusinessId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public uint DisplayOrder { get; set; }

    public bool? IsActive { get; set; }

    public virtual Business Business { get; set; } = null!;

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
