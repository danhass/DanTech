using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DanTech.Data;

namespace DTUserManagement.Services
{
    public interface IDTRegistration
    {
        public string RegistrationKey();
        public void SetConfig(IConfiguration config);
        public dtRegistration? SendRegistration(string email, string baseUrl);
        public string CompleteRegistration(string email, string regKey, string hostAddress);
    }
}
