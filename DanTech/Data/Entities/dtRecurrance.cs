using System;
using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtRecurrance
    {
        public dtRecurrance()
        {
            dtPlanItems = new HashSet<dtPlanItem>();
        }

        public int id { get; set; }
        public string title { get; set; }
        public string note { get; set; }
        public string description { get; set; }
        public DateTime? effective { get; set; }
        public DateTime? stops { get; set; }
        public int? daysToPopulate { get; set; }

        public virtual ICollection<dtPlanItem> dtPlanItems { get; set; }
    }
}
