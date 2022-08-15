using System;
using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtUser
    {
        public dtUser()
        {
            dtPlanItems = new HashSet<dtPlanItem>();
            dtProjects = new HashSet<dtProject>();
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

        public virtual dtType typeNavigation { get; set; }
        public virtual dtSession dtSession { get; set; }
        public virtual ICollection<dtPlanItem> dtPlanItems { get; set; }
        public virtual ICollection<dtProject> dtProjects { get; set; }
    }
}
