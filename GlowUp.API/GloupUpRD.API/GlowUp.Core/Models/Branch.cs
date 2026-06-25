using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Branch
{
    public ulong Id { get; set; }

    public ulong BusinessId { get; set; }

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string AddressLine { get; set; } = null!;

    public string City { get; set; } = null!;

    public string Province { get; set; } = null!;

    public string Country { get; set; } = null!;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public bool IsMain { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Business Business { get; set; } = null!;

    public virtual ICollection<BusinessHour> BusinessHours { get; set; } = new List<BusinessHour>();

    public virtual ICollection<BusinessMember> BusinessMembers { get; set; } = new List<BusinessMember>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
