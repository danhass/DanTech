using DanTech.Data;
using DanTech.Data.Models;
using EAGetMail;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace DanTech.Services
{
    public interface IDTGmailService
    {
        void SetConfig(IConfiguration cfg);
        string GetAuthToken();
        void SetAuthToken(string authToken);
        void SetRefreshToken(string refreshToken);
        bool SetMailMessage(string license, string from, List<string> to, string subject, string body, string html, List<string> attachments);
        public Imap4Folder FindFolder(string folderPath, Imap4Folder[] folders);
        bool Send();
    }
}
