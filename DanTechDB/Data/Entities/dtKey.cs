using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtKey
{
    public int id { get; set; }

    public string key { get; set; } = null!;

    public string? note { get; set; }

    public virtual ICollection<dtAuthorization> dtAuthorizations { get; set; } = new List<dtAuthorization>();
}
