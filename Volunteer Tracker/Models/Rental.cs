using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Аренда ресурсов студентами
/// </summary>
public partial class Rental
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int ResourceId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? Status { get; set; }

    public int? DurationMinutes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Resource Resource { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
