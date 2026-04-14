using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Настройки интеграций с внешними системами
/// </summary>
public partial class IntegrationSetting
{
    public int Id { get; set; }

    public string IntegrationName { get; set; } = null!;

    public string Settings { get; set; } = null!;

    public bool? IsEnabled { get; set; }

    public DateTime? LastSyncAt { get; set; }

    public string? LastSyncStatus { get; set; }

    public string? LastSyncError { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
