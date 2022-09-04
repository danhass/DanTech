using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DanTech.Models.Data
{
    public class dtRecurrenceModel
    {
        public int id { get; set; }
        public string title { get; set; }
        public string note { get; set; }
        public string description { get; set; }
        public DateTime? effective { get; set; }
        public DateTime? stops { get; set; }
        public int? daysToPopulate { get; set; }
    }
}
