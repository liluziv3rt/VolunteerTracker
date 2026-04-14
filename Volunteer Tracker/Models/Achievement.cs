using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Достижения для геймификации
/// </summary>
public partial class Achievement
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string TriggerType { get; set; } = null!;

    public int ThresholdValue { get; set; }

    public string? BadgeIcon { get; set; }

    public string? BadgeColor { get; set; }

    public int? BonusPoints { get; set; }

    public string? Rarity { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
