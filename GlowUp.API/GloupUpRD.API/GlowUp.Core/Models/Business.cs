using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Business
{
    public ulong Id { get; set; }

    public ulong OwnerUserId { get; set; }

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string BusinessType { get; set; } = null!;

    public string? Description { get; set; }

    public string? Rnc { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? LogoUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();

    public virtual ICollection<BusinessCustomer> BusinessCustomers { get; set; } = new List<BusinessCustomer>();

    public virtual ICollection<BusinessMember> BusinessMembers { get; set; } = new List<BusinessMember>();

    public virtual ICollection<BusinessSubscription> BusinessSubscriptions { get; set; } = new List<BusinessSubscription>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual User OwnerUser { get; set; } = null!;

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<ServiceCategory> ServiceCategories { get; set; } = new List<ServiceCategory>();

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}
