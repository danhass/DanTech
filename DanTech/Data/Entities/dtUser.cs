using System;
using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtUser
    {
        public dtUser()
        {
            dtConfigs = new HashSet<dtConfig>();
            dtPlanItems = new HashSet<dtPlanItem>();
            dtProjects = new HashSet<dtProject>();
            dtSessions = new HashSet<dtSession>();
        }

        public int id { get; set; }
        public int type { get; set; }
        public string fName { get; set; }
        public string lName { get; set; }
        public string otherName { get; set; }
        public string email { get; set; }
        public string token { get; set; }
        public string refreshToken { get; set; }
        public DateTime? lastLogin { get; set; }
        public byte? suspended { get; set; }
        public DateTime? updated { get; set; }
        public string pw { get; set; }

        public virtual dtType typeNavigation { get; set; }
        public virtual ICollection<dtConfig> dtConfigs { get; set; }
        public virtual ICollection<dtPlanItem> dtPlanItems { get; set; }
        public virtual ICollection<dtProject> dtProjects { get; set; }
        public virtual ICollection<dtSession> dtSessions { get; set; }
    }
}
