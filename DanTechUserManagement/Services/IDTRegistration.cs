using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTUserManagement.Services
{
    public interface IDTRegistration
    {
        public bool SendRegistration(string email);
    }
}
