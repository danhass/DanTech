using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DanTech.Data;
using DanTech.Models.Data;

namespace DanTech.Models
{
    public class DTViewModel
    {
        public string StatusMessage { get; set; }
        public  dtUserModel User { get; set; }
        public bool TestEnvironment { get; set; }
        public bool IsTesting { get; set; }
    }
}
