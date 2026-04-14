using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Учёт волонтёрских часов студентов
/// </summary>
public partial class VolunteerHour
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? ProjectId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public decimal? Hours { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public int? ConfirmedBy { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? PointsAwarded { get; set; }

    public virtual User? ConfirmedByNavigation { get; set; }

    public virtual Project? Project { get; set; }

    public virtual User User { get; set; } = null!;
}
