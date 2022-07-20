using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DanTech.Data;
    
namespace DanTech.Models.Data
{
    public class dtSessionModel
    {  
        public int id { get; set; }
        public int user { get; set; }
        public string session { get; set; }
        public string hostAddress { get; set; }
        public DateTime expires { get; set; }
    }
}
