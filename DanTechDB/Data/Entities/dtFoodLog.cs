using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtFoodLog
{
    public int id { get; set; }

    public int food { get; set; }

    public decimal? quantity { get; set; }

    public DateTime ts { get; set; }

    public int owner { get; set; }

    public string? note { get; set; }

    public virtual dtFood foodNavigation { get; set; } = null!;

    public virtual dtUser ownerNavigation { get; set; } = null!;
}
