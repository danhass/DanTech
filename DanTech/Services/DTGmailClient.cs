using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace DanTech.Services
{
    public class DTGmailClient : IDTGmailClient
    { 
        private IConfiguration _cfg;
        public DTGmailClient(IConfiguration cfg)
        {
            _cfg = cfg;
        }

        public bool Send()
        {
            return true;
        }
    }
}
