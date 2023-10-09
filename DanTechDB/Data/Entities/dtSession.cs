using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtSession
{
    public int id { get; set; }

    public int user { get; set; }

    public string session { get; set; } = null!;

    public string hostAddress { get; set; } = null!;

    public DateTime expires { get; set; }

    public virtual dtUser userNavigation { get; set; } = null!;
}
