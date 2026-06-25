using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Service
{
    public ulong Id { get; set; }

    public ulong BusinessId { get; set; }

    public ulong? CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public uint DurationMinutes { get; set; }

    public decimal Price { get; set; }

    public uint BufferBeforeMinutes { get; set; }

    public uint BufferAfterMinutes { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();

    public virtual Business Business { get; set; } = null!;

    public virtual ServiceCategory? Category { get; set; }

    public virtual ICollection<EmployeeService> EmployeeServices { get; set; } = new List<EmployeeService>();
}
