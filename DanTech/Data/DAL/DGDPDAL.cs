using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DanTech.Data
{
    public class DGDPDAL : IDGDPDAL
    {
        private dgdb _DB = null;

        public DGDPDAL(dgdb aDB)
        {
            _DB = aDB;
        }
    }
}
