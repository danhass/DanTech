using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtType
{
    public int id { get; set; }

    public string title { get; set; } = null!;

    public string? description { get; set; }

    public virtual ICollection<dtConfig> dtConfigs { get; set; } = new List<dtConfig>();

    public virtual ICollection<dtUser> dtUsers { get; set; } = new List<dtUser>();
}
