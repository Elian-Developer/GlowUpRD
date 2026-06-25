using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class AuditLog
{
    public ulong Id { get; set; }

    public ulong? UserId { get; set; }

    public ulong? BusinessId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public ulong? EntityId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Business? Business { get; set; }

    public virtual User? User { get; set; }
}
