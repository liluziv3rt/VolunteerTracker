using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

public partial class StudentRanking
{
    public int? Id { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? GroupName { get; set; }

    public int? TotalPoints { get; set; }

    public decimal? TotalHours { get; set; }

    public long? BadgesCount { get; set; }

    public long? Rank { get; set; }
}
