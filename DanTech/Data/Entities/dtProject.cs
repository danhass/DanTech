using System;
using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtProject
    {
        public dtProject()
        {
            dtPlanItems = new HashSet<dtPlanItem>();
        }

        public int id { get; set; }
        public string title { get; set; }
        public string shortCode { get; set; }
        public string notes { get; set; }
        public int user { get; set; }
        public int? priority { get; set; }
        public int? sortOrder { get; set; }
        public int? colorCode { get; set; }

        public virtual dtColorCode colorCodeNavigation { get; set; }
        public virtual dtUser userNavigation { get; set; }
        public virtual ICollection<dtPlanItem> dtPlanItems { get; set; }
    }
}
