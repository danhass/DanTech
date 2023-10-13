using DanTech.Data;
using DanTech.Data.Models;
using EASendMail;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;

namespace DanTech.Services
{
    public class DTGmailService : IDTGmailService
    {
        private static IConfiguration? _config = null;
        public static readonly string ConfigSection = "Gmail";
        private static string _authToken = string.Empty;
        private static SmtpMail? _message = null;

        public void SetConfig(IConfiguration? cfg)
        {
            _config = cfg;
        }
        public void SetAuthToken(string authToken)
        {
            _authToken = authToken;
        }
        public bool SetMailMessage(string license, string from, List<string> to, string subject, string body, string html, List<string> attachments)
        {
            if (string.IsNullOrEmpty(license)) license = "TryIt";
            _message = new SmtpMail(license);
            _message.From = new MailAddress(from);
            foreach (var r in to)
            {
                _message.To.Add(new MailAddress(r));
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

        public bool Send()
        {
            if (_config == null) throw new Exception("Config not set!");
            if (string.IsNullOrEmpty(_authToken)) throw new Exception("Authorization token not set!");
            if (_message == null) throw new Exception("Message not set!");

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
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Failed to send email with the following error: {0}", ex.Message));
            }

            return true;

        }
    }
}
