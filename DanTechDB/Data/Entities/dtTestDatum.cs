using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtTestDatum
{
    public int id { get; set; }

    public string title { get; set; } = null!;

    public string? value { get; set; }
}
