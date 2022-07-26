using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DanTech.Models.Data
{
    public class dtProjectModel
    {
        public int id { get; set; }
        public string title { get; set; }
        public string shortCode { get; set; }
        public string notes { get; set; }
        public int? priority { get; set; }
        public int? sortOrder { get; set; }
        public dtUserModel user { get; set; }
    }
}
