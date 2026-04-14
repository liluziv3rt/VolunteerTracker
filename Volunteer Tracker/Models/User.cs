using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Пользователи системы (студенты, руководители, администраторы)
/// </summary>
public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string? GroupName { get; set; }

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = null!;

    public bool? IsActive { get; set; }

    public int? AmoCrmId { get; set; }

    public string? ExternalApiId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();

    public virtual UserPoint? UserPoint { get; set; }

    public virtual ICollection<VolunteerHour> VolunteerHourConfirmedByNavigations { get; set; } = new List<VolunteerHour>();

    public virtual ICollection<VolunteerHour> VolunteerHourUsers { get; set; } = new List<VolunteerHour>();
}
