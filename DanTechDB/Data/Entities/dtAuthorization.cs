using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtAuthorization
{
    public int id { get; set; }

    public int user { get; set; }

    public int key { get; set; }

    public virtual dtKey keyNavigation { get; set; } = null!;

    public virtual dtUser userNavigation { get; set; } = null!;
}
