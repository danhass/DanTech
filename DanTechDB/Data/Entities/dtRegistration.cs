using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtRegistration
{
    public int id { get; set; }

    public string email { get; set; } = null!;

    public string regKey { get; set; } = null!;

    public DateTime created { get; set; }
}
