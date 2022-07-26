using System;
using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtProject
    {
        public int id { get; set; }
        public string title { get; set; }
        public string shortCode { get; set; }
        public string notes { get; set; }
        public int user { get; set; }
        public int? priority { get; set; }
        public int? sortOrder { get; set; }

        public virtual dtUser userNavigation { get; set; }
    }
}
