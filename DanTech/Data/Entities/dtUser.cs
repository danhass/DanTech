using System;
using System.Collections.Generic;

namespace DanTech.Data;

public partial class dtUser
{
    public int id { get; set; }

    public int type { get; set; }

    public string fName { get; set; }

    public string lName { get; set; }

    public string otherName { get; set; }

    public string email { get; set; }

    public string token { get; set; }

    public string refreshToken { get; set; }

    public DateTime? lastLogin { get; set; }

    public sbyte? suspended { get; set; }

    public DateTime? updated { get; set; }

    public string pw { get; set; }

    public virtual ICollection<dtAuthorization> dtAuthorizations { get; set; } = new List<dtAuthorization>();

    public virtual ICollection<dtConfig> dtConfigs { get; set; } = new List<dtConfig>();

    public virtual ICollection<dtPlanItem> dtPlanItems { get; set; } = new List<dtPlanItem>();

    public virtual ICollection<dtProject> dtProjects { get; set; } = new List<dtProject>();

    public virtual ICollection<dtSession> dtSessions { get; set; } = new List<dtSession>();

    public virtual dtType typeNavigation { get; set; }
}
