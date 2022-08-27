using System;
using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtType
    {
        public dtType()
        {
            dtConfigs = new HashSet<dtConfig>();
            dtUsers = new HashSet<dtUser>();
        }

        public int id { get; set; }
        public string title { get; set; }
        public string description { get; set; }

        public virtual ICollection<dtConfig> dtConfigs { get; set; }
        public virtual ICollection<dtUser> dtUsers { get; set; }
    }
}
