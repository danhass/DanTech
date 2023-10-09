using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtRecurrence
{
    public int id { get; set; }

    public string title { get; set; }

    public string note { get; set; }

    public string description { get; set; }

    public DateTime? effective { get; set; }

    public DateTime? stops { get; set; }

    public int? daysToPopulate { get; set; }

    public virtual ICollection<dtPlanItem> dtPlanItems { get; set; } = new List<dtPlanItem>();
}
