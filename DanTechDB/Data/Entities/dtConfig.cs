﻿using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtConfig
{
    public int id { get; set; }

    public int key { get; set; }

    public string? value { get; set; }

    public int type { get; set; }

    public int user { get; set; }

    public virtual dtType typeNavigation { get; set; } = null!;

    public virtual dtUser userNavigation { get; set; } = null!;
}
