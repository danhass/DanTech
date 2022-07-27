using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DanTech.Models.Data
{
    public class dtPlanItemModel
    {
        public int? id { get; set; }
        public string title { get; set; }
        public string note { get; set; }
        public DateTime day { get; set; }
        public DateTime? start { get; set; }
        public TimeSpan? duration { get; set; }
        public int? priority { get; set; }
        public bool? addToCalendar { get; set; }
        public bool? completed { get; set; }
        public dtProjectModel? project { get; set; }
        public dtUserModel user { get; set; }
    }
}
