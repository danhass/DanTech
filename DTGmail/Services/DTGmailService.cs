using DanTech.Data;
using DanTech.Data.Models;
using EASendMail;
using EAGetMail;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using DanTech.DTGoogleAuth;

namespace DanTech.Services
{
    public class DTGmailService : IDTGmailService
    {
        private static IConfiguration? _config = null;
        public static readonly string ConfigSection = "Gmail";
        private static string _authToken = string.Empty;
        private static string _refreshToken = string.Empty;
        private static SmtpMail? _message = null;

        public int DeleteFromFolder(string folderName = "Sent Mail")
        {
            if (_config == null) throw new Exception("Config not set!");
            if (string.IsNullOrEmpty(_authToken)) throw new Exception("Authorization token not set!");
            int ct = 0;
            bool done = false;
            bool refreshed = false;
            while (!done)
            {

                try
                {
                    MailServer oServer = new MailServer("imap.gmail.com",
                                    _config.GetValue<string>("Gmail:Email"),
                                    _authToken,
                                    EAGetMail.ServerProtocol.Imap4);
                    oServer.AuthType = ServerAuthType.AuthXOAUTH2;

                    // Enable SSL connection.
                    oServer.SSLConnection = true;

                    // Set 993 SSL port
                    oServer.Port = 993;

                    MailClient oClient = new MailClient("TryIt");
                    oClient.Connect(oServer);
                    Imap4Folder[] folders = oClient.GetFolders();
                    var folder = FindFolder(folderName, folders);
                    oClient.SelectFolder(folder);

                    var trashFolder = FindFolder("Trash", folders);

                    MailInfo[] infos = oClient.GetMailInfos();
                    string buf = string.Empty;

                    for (int i = 0; i < infos.Length; i++)
                    {
                        oClient.Move(infos[i], trashFolder);
                        ct++;
                    }
                    done = true;
                }
                catch (Exception ex)
                {
                    if (refreshed || string.IsNullOrEmpty(_refreshToken)) throw new Exception(string.Format("Could not delete from folder: {0}", ex.Message));
                }

                if (!done)
                {
                    refreshed = RefreshToken();
                }
            }
            return ct;
        }

        public void SetConfig(IConfiguration? cfg)
        {
            _config = cfg;
        }
        public string GetAuthToken() { return  _authToken; }
        public void SetAuthToken(string authToken)
        {
            _authToken = authToken;
        }
        public void SetRefreshToken(string refreshToken) { _refreshToken = refreshToken; }
        public bool SetMailMessage(string license, string from, List<string> to, string subject, string body, string html, List<string> attachments)
        {
            if (string.IsNullOrEmpty(license)) license = "TryIt";
            _message = new SmtpMail(license);
            _message.From = new EASendMail.MailAddress(from);
            foreach (var r in to)
            {
                _message.To.Add(new EASendMail.MailAddress(r));
            }
            _message.Subject = subject;
            _message.TextBody = body;
            _message.HtmlBody = html;
            foreach (var a in attachments)
            {
                _message.AddAttachment(a);
            }

            return true;
        }
        public Imap4Folder FindFolder(string folderName, Imap4Folder[] folders)
        {
            int count = folders.Length;
            for (int i = 0; i < count; i++)
            {
                Imap4Folder folder = folders[i];
                if (folder.Name == folderName)
                {
                    return folder;
                }

                folder = FindFolder(folderName, folder.SubFolders);
                if (folder != null)
                {
                    return folder;
                }
            }

            // No folder found
            return null;
        }
        private bool RefreshToken()
        {
            try
            {
                var auth = new DTGoogleAuthService();
                auth.SetConfig(_config);
                _authToken = auth.RefreshAuthToken(_refreshToken, new List<string>() { DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleCalendarScope, DTGoogleAuthService.GoogleMailScope, DTGoogleAuthService.GoogleGmailSendScope, DTGoogleAuthService.GoogleGmailModifyScope }, true);
            }
            catch (Exception ex) { throw new Exception(string.Format("Unable to refresh auth token: {0}", ex.Message)); }
            return true;
        }
        public bool Send()
        {
            if (_config == null) throw new Exception("Config not set!");
            if (string.IsNullOrEmpty(_authToken)) throw new Exception("Authorization token not set!");
            if (_message == null) throw new Exception("Message not set!");
            bool confirmed = false;
            bool done = false;
            bool refreshed = false;
            while (!done)
            {
                try
                {
                    SmtpClient oSmtp = new SmtpClient();
                    SmtpServer oServer = new SmtpServer("smtp.gmail.com");
                    oServer.Port = 587;
                    oServer.ConnectType = SmtpConnectType.ConnectSSLAuto;
                    oServer.AuthType = SmtpAuthType.XOAUTH2;
                    oServer.User = _config.GetValue<string>("Gmail:Email")!;
                    oServer.Password = _authToken;

                    oSmtp.SendMail(oServer, _message);
                    done = true;
                }
                catch (Exception ex)
                {
                    if (refreshed || string.IsNullOrEmpty(_refreshToken)) throw new Exception(string.Format("Failed to send email with the following error: {0}", ex.Message));                    
                }

                if (!done)
                {
                    refreshed = RefreshToken();
                }
            }        

            try
            {
                MailServer oServer = new MailServer("imap.gmail.com",
                                _config.GetValue<string>("Gmail:Email"),
                                _authToken,
                                EAGetMail.ServerProtocol.Imap4);
                oServer.AuthType = ServerAuthType.AuthXOAUTH2;

                // Enable SSL connection.
                oServer.SSLConnection = true;

                // Set 993 SSL port
                oServer.Port = 993;

                MailClient oClient = new MailClient("TryIt");
                oClient.Connect(oServer);
                Imap4Folder[] folders = oClient.GetFolders();
                var folder = FindFolder("Sent Mail", folders);
                oClient.SelectFolder(folder);

                MailInfo[] infos = oClient.GetMailInfos();
                string buf = string.Empty;

                for (int i = 0; i < infos.Length && !confirmed; i++)
                {
                    var oMail = oClient.GetMail(infos[i]);
                    if (oMail != null)
                    {
                        bool mailsMatch = true;
                        buf = oMail.Subject.Replace(" (Trial Version)", "");
                        if (buf != _message.Subject) mailsMatch = false;
                        for (int j = 0; j < oMail.To.Length && mailsMatch; j++)
                        {
                            if (j >= _message.To.Count || oMail.To[j].Address != _message.To[j].Address) mailsMatch = false;
                        }
                        if (mailsMatch)
                        {
                            confirmed = true;
                        }
                    }
                }
            }
            catch (Exception ex) { throw new Exception(string.Format("Could not verify send", ex.Message)); }
            return confirmed;
        }
    }
}
