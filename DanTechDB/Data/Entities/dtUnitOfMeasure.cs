using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtUnitOfMeasure
{
    public int id { get; set; }

    public string title { get; set; } = null!;

    public string? abbrev { get; set; }

    public virtual ICollection<dtFood> dtFoods { get; set; } = new List<dtFood>();
}
