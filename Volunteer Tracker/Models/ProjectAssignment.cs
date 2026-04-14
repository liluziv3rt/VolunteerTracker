using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Назначения студентов на проекты
/// </summary>
public partial class ProjectAssignment
{
    public int Id { get; set; }

    public int ProjectId { get; set; }

    public int UserId { get; set; }

    public string? Status { get; set; }

    public int? PointsEarned { get; set; }

    public string? RoleInProject { get; set; }

    public DateTime? JoinedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
