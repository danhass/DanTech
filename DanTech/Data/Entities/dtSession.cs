using System;
using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtSession
    {
        public int id { get; set; }
        public int user { get; set; }
        public string session { get; set; }
        public string hostAddress { get; set; }
        public DateTime expires { get; set; }

        public virtual dtUser userNavigation { get; set; }
    }
}
