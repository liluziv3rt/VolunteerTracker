using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Проекты, в которых участвуют студенты
/// </summary>
public partial class Project
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public int LeaderId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateOnly? RegistrationDeadline { get; set; }

    public int? MaxPoints { get; set; }

    public int? PointsPerHour { get; set; }

    public string? Status { get; set; }

    public string? Category { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User Leader { get; set; } = null!;

    public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();

    public virtual ICollection<VolunteerHour> VolunteerHours { get; set; } = new List<VolunteerHour>();
}
