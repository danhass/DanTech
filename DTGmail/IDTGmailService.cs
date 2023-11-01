using DanTech.Data;
using DanTech.Data.Models;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace DanTech.Services
{
    public interface IDTGmailService
    {
        void SetConfig(IConfiguration cfg);
        void SetAuthToken(string authToken);
        bool SetMailMessage(string license, string from, List<string> to, string subject, string body, string html, List<string> attachments);
        bool Send();
    }
}
