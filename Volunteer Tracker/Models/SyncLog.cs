using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Журнал синхронизации с внешними API
/// </summary>
public partial class SyncLog
{
    public int Id { get; set; }

    public string IntegrationName { get; set; } = null!;

    public string SyncType { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? RecordsProcessed { get; set; }

    public int? RecordsFailed { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}
