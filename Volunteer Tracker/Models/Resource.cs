using System;
using System.Collections.Generic;

namespace Volunteer_Tracker.Models;

/// <summary>
/// Ресурсы для аренды (рабочие станции, оборудование)
/// </summary>
public partial class Resource
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? Description { get; set; }

    public string? SerialNumber { get; set; }

    public bool? IsAvailable { get; set; }

    public string? Location { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Rental> Rentals { get; set; } = new List<Rental>();
}
