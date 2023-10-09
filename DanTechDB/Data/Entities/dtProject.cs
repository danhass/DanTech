using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtProject
{
    public int id { get; set; }

    public string title { get; set; } = null!;

    public string shortCode { get; set; } = null!;

    public string? notes { get; set; }

    public int user { get; set; }

    public int? priority { get; set; }

    public int? sortOrder { get; set; }

    public int? colorCode { get; set; }

    public int status { get; set; }

    public virtual dtColorCode? colorCodeNavigation { get; set; }

    public virtual ICollection<dtPlanItem> dtPlanItems { get; set; } = new List<dtPlanItem>();

    public virtual dtStatus statusNavigation { get; set; } = null!;

    public virtual dtUser userNavigation { get; set; } = null!;
}
