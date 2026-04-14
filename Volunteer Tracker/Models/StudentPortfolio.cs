using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

public partial class StudentPortfolio
{
    public int? UserId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? GroupName { get; set; }

    public string? ActivityType { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public int? PointsChange { get; set; }

    public decimal? HoursChange { get; set; }

    public DateTime? CreatedAt { get; set; }
}
