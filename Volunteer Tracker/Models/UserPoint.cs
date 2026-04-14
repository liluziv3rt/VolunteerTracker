using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Суммарная статистика пользователей
/// </summary>
public partial class UserPoint
{
    public int UserId { get; set; }

    public int? TotalPoints { get; set; }

    public decimal? TotalVolunteerHours { get; set; }

    public int? TotalProjectsCompleted { get; set; }

    public int? TotalRentalMinutes { get; set; }

    public int? CurrentStreakDays { get; set; }

    public int? MaxStreakDays { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
