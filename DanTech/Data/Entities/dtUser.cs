using System;
using System.Collections.Generic;

#nullable disable

namespace DanTech.Data
{
    public partial class dtUser
    {
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
    }
}
