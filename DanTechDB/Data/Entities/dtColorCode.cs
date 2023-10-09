using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtColorCode
{
    public int id { get; set; }

    public string title { get; set; } = null!;

    public string? note { get; set; }

    public virtual ICollection<dtProject> dtProjects { get; set; } = new List<dtProject>();

    public virtual ICollection<dtStatus> dtStatuses { get; set; } = new List<dtStatus>();
}
