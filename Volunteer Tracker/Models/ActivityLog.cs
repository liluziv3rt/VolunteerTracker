using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Журнал активности для зачётной книжки
/// </summary>
public partial class ActivityLog
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string ActivityType { get; set; } = null!;

    public int? ReferenceId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? PointsChange { get; set; }

    public decimal? HoursChange { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
