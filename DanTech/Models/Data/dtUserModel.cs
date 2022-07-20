using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DanTech.Data;

namespace DanTech.Models.Data
{
    public class dtUserModel
    { 

        public int id { get; set; }
        public string fName { get; set; }
        public string lName { get; set; }
        public string otherName { get; set; }
        public string email { get; set; }
        public string token { get; set; }
        public string refreshToken { get; set; }
        public DateTime? lastLogin { get; set; }
        public byte? suspended { get; set; }

        public dtSessionModel session { get; set; }

    }
}
