using System;
using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtPlanItem
    {
        public int id { get; set; }
        public int user { get; set; }
        public int? project { get; set; }
        public string title { get; set; }
        public string note { get; set; }
        public DateTime day { get; set; }
        public DateTime? start { get; set; }
        public TimeSpan? duration { get; set; }
        public int? priority { get; set; }
        public bool? addToCalendar { get; set; }
        public bool? completed { get; set; }
        public bool? preserve { get; set; }

        public virtual dtProject projectNavigation { get; set; }
        public virtual dtUser userNavigation { get; set; }
    }
}
