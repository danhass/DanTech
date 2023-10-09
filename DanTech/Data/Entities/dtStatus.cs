using System;
using System.Collections.Generic;

namespace DanTech.Data;

/// <summary>
/// This is a system table. Users do not make custom stati.
/// </summary>
public partial class dtStatus
{
    public int id { get; set; }

    public string title { get; set; }

    public string note { get; set; }

    public int? colorCode { get; set; }

    public virtual dtColorCode colorCodeNavigation { get; set; }

    public virtual ICollection<dtProject> dtProjects { get; set; } = new List<dtProject>();
}
