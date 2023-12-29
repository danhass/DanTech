using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtFood
{
    public int id { get; set; }

    public int? owner { get; set; }

    public string title { get; set; } = null!;

    public decimal servingSize { get; set; }

    public int unitType { get; set; }

    public decimal? fat { get; set; }

    public decimal? protein { get; set; }

    public decimal? carb { get; set; }

    public decimal? fiber { get; set; }

    public virtual ICollection<dtFoodLog> dtFoodLogs { get; set; } = new List<dtFoodLog>();

    public virtual dtUser? ownerNavigation { get; set; }

    public virtual dtUnitOfMeasure unitTypeNavigation { get; set; } = null!;
}
