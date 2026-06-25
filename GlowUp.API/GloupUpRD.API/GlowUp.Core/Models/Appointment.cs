using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Appointment
{
    public ulong Id { get; set; }

    public ulong BusinessId { get; set; }

    public ulong BranchId { get; set; }

    public ulong CustomerId { get; set; }

    public ulong? BusinessCustomerId { get; set; }

    public ulong EmployeeId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public string Status { get; set; } = null!;

    public string? CancellationReason { get; set; }

    public string? Notes { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();

    public virtual Branch Branch { get; set; } = null!;

    public virtual Business Business { get; set; } = null!;

    public virtual BusinessCustomer? BusinessCustomer { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Review? Review { get; set; }
}
