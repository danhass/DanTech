using DanTech.Data;
using DanTech.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZstdSharp.Unsafe;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace DTUserManagement.Services
{
    public class DTRegistration : IDTRegistration
    {
        IDTDBDataService? _db = null;
        private static IConfiguration? _config = null;
        private static readonly string _defaultBaseUrl = @"https://7822-54268.el-alt.com";

        public DTRegistration() { }
        public DTRegistration(IDTDBDataService db)
        {
            _db = db;
        }
        public string CompleteRegistration(string email, string regKey, string hostAddress)
        {
            string session = "";
            if (_db == null) return session;
            var reg = _db.Registrations.Where(x => x.email == email && x.regKey == regKey).FirstOrDefault();
            if (reg == null) return session;
            var user = _db.Users.Where(x => x.email == email).FirstOrDefault();
            if (user == null)
            {
                user = new dtUser();
                user.email = email;
                user.type = 1;
                user = _db.Set(user);
            }
            if (user == null || user.id <= 0) return session;
            var s = _db.Sessions.Where(x=> x.user == user.id).FirstOrDefault();
            if (s == null)
            {
                s = new dtSession();
                s.user = user.id;
                s.session = Guid.NewGuid().ToString();
            }
            s.expires = DateTime.Now.AddDays(7);
            s.hostAddress = hostAddress;
            s = _db.Set(s);
            session = s.session;
            _db.Delete(reg);
            return session;
        }
        private string RegistrationMessage(string baseUrl, string targetEmail, string regKey)
        {
            string message = "To complete your registration to DanTech's platform, click on the following link: " +
                baseUrl + "/Admin/CompleteRegistrtation?email=" + targetEmail + "&regKey=" + regKey;
            return message;
        }
        public void SetConfig(IConfiguration config) { _config = config;  }
        public dtRegistration? SendRegistration(string email, string baseUrl = "")
        {
            dtRegistration? reg = null;
            if (_db == null) throw new Exception("No db specified.");
            if (_config == null) throw new Exception("No config file specified");
            if (string.IsNullOrEmpty(baseUrl)) baseUrl = _defaultBaseUrl;
            var svc = new DTGmailService();
            svc.SetConfig(_config);
            var userEmail = _config.GetValue<string>("Gmail:Email");
            var gmailUser = _db.Users.Where(x => x.email == userEmail).FirstOrDefault();
            if (gmailUser != null)
            {
                svc.SetAuthToken(gmailUser.token!);
                svc.SetRefreshToken(gmailUser.refreshToken!);
                var regKey = RegistrationKey();
                var message = RegistrationMessage(baseUrl, email, regKey);
                svc.SetMailMessage("TryIt", gmailUser.email!, new List<string>() { email }, "DanTech Registration", message, message, new List<string>());
                if (svc.Send())
                {
                    reg = new dtRegistration(); 
                    reg.email = email;
                    reg.regKey = regKey;
                    reg.created = DateTime.Now; ;
                    _db.Set(reg);
                }
            }
            return reg;
        }
        public string RegistrationKey()
        {
            Random rnd = new Random();
            int seed = rnd.Next(100000, 1000000);
            var rVal = string.Format("{0,6}", seed);
            return rVal;
        }
    }
}
