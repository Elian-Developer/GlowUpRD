using System;
using System.Collections.Generic;

namespace GloupUpRD.API.Models;

public partial class Employee
{
    public ulong Id { get; set; }

    public ulong BusinessId { get; set; }

    public ulong? BranchId { get; set; }

    public ulong? UserId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Position { get; set; }

    public string? Bio { get; set; }

    public string? PhotoUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual Branch? Branch { get; set; }

    public virtual Business Business { get; set; } = null!;

    public virtual ICollection<EmployeeSchedule> EmployeeSchedules { get; set; } = new List<EmployeeSchedule>();

    public virtual ICollection<EmployeeService> EmployeeServices { get; set; } = new List<EmployeeService>();

    public virtual ICollection<EmployeeTimeOff> EmployeeTimeOffs { get; set; } = new List<EmployeeTimeOff>();

    public virtual User? User { get; set; }
}
