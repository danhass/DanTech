using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Oauth2.v2.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DanTech.Data;
using Google.Apis.Services;
using DanTech.Models.Data;

namespace DanTech.Services
{
    public class DTGoogleAuthService : IDTGoogleAuthService
    {
        private static readonly string GoogleAuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private static readonly string GoogleCalendarScope = "https://www.googleapis.com/auth/calendar";
        private static readonly string GoogleUserInfoEmailScope = "https://www.googleapis.com/auth/userinfo.email";
        private static readonly string GoogleUserInfoProfileScope = "https://www.googleapis.com/auth/userinfo.profile";

        public string AuthService(string returnDomain, string returnHandler, IConfiguration config)
        {
            string r = GoogleAuthEndpoint + "?" +
                "scope=" + GoogleUserInfoEmailScope + " " + GoogleUserInfoProfileScope + " " + GoogleCalendarScope +
                "&state=google_signin" +
                "&redirect_uri=https://" + returnDomain + (returnHandler.StartsWith('/') ? "" : "/") + returnHandler +
                "&access_type=offline" +
                "&response_type=code" +
                "&client_id=" + config.GetValue<string>("Google:ClientId");

            return r;
        }

        public string RefreshAuthToken(string refreshToken)
        {
            string tokenString = "";
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
            var clientSecrets = new ClientSecrets
            {
                ClientId = config.GetValue<string>("Google:ClientId"),
                ClientSecret = config.GetValue<string>("Google:ClientSecret")
            };
            var credential = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets,
                    Scopes = new string[] { GoogleUserInfoEmailScope, GoogleUserInfoProfileScope, GoogleCalendarScope }
                }
            );
            try
            {
                var res = credential.RefreshTokenAsync("", refreshToken, System.Threading.CancellationToken.None).Result;
                tokenString = res.AccessToken;
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
            }
            return tokenString;
        }

        public Dictionary<string, string> AuthToken(string code, string domain, string endPoint = "")
        {
            if (string.IsNullOrEmpty(endPoint) || domain.StartsWith("https://localhost:44324")) endPoint = "/Home/GoogleSignin";
            Dictionary<string, string> res = new Dictionary<string, string>();
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").AddEnvironmentVariables().Build();
            var clientSecrets = new ClientSecrets
            {
                ClientId = config.GetValue<string>("Google:ClientId"),
                ClientSecret = config.GetValue<string>("Google:ClientSecret")
            };

            var credential = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets,
                    Scopes = new string[] { GoogleUserInfoEmailScope, GoogleUserInfoProfileScope, GoogleCalendarScope }
                }
            );

            try
            {
                TokenResponse token = credential.ExchangeCodeForTokenAsync(
                    "",
                    code,
                    domain + endPoint,
                    System.Threading.CancellationToken.None).Result;

                res["AccessToken"] = token.AccessToken;
                res["RefreshToken"] = token.RefreshToken;
            }
            catch(Exception ex)
            {
                res["AccessToken"] = "";
                res["RefreshToken"] = "";
                string msgs = ex.Message;
            }

            return res;
        }

        public Userinfo GetUserInfo (string token, string refreshToken = "")
        {
            var cred = GoogleCredential.FromAccessToken(token, null);
            var oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer()
            {
                HttpClientInitializer = cred
            });
            Userinfo userInfo = null;
            try
            {
                userInfo  = oauthSerivce.Userinfo.Get().Execute();
            }
            catch(Exception)
            {
                // Retry with refresh token
            }
            if ((userInfo == null || string.IsNullOrEmpty(userInfo.Email)) && !string.IsNullOrEmpty(refreshToken))
            {

                cred = GoogleCredential.FromAccessToken(RefreshAuthToken(refreshToken));
                oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer() { HttpClientInitializer = cred });
                try
                {
                    userInfo = oauthSerivce.Userinfo.Get().Execute();
                }
                catch(Exception)
                {
                    // Weren't able to retrieve with either token. Just return the null.
                }
            }
            return userInfo;
        }

        public dtLogin SetLogin(string sessionId, string hostAddress, dtdb db, ref string log)
        {
            dtLogin login = new dtLogin();
            var outOfDates = (from x in db.dtSessions where x.expires < DateTime.Now select x).ToList();
            db.RemoveRange(outOfDates);
            var session = (from x in db.dtSessions where x.session == sessionId && x.hostAddress == hostAddress select x).FirstOrDefault();
            if (session != null)
            {
                login.Session = session.session;
                var user = (from x in db.dtUsers where x.id == session.user select x).FirstOrDefault();
                if (user != null)
                {
                    login.Email = user.email;
                    login.FName = user.fName;
                    login.LName = user.lName;
                }
            }
            else
            {
                log += "Invalid session";
                login.Message = log;
            }
            return login;
        }

        public dtLogin SetLogin(Userinfo userInfo, HttpContext ctx, dtdb db, string accessToken, string refreshToken)
        {
            dtLogin login = new dtLogin();
            Guid sessionGuid = Guid.NewGuid();
            if (userInfo != null && !string.IsNullOrEmpty(userInfo.Email) && !(string.IsNullOrEmpty(userInfo.GivenName) && string.IsNullOrEmpty(userInfo.FamilyName)))
            {
                var user = (from x in db.dtUsers where x.email.ToLower() == userInfo.Email.ToLower() select x).FirstOrDefault();
                if (user == null)
                {
                    user = new dtUser() { type = 1 };
                    db.dtUsers.Add(user);
                }
                user.email = userInfo.Email;
                user.fName = userInfo.GivenName;
                user.lName = userInfo.FamilyName;
                user.lastLogin = DateTime.Now;
                user.token = accessToken;
                user.refreshToken = refreshToken;
                db.SaveChanges();
                login.Email = userInfo.Email;
                login.FName = userInfo.GivenName;
                login.LName = userInfo.FamilyName;

                var hostAddress = ctx.Request.Host.Value;

                var session = (from x in db.dtSessions where x.user == user.id && x.hostAddress == hostAddress select x).FirstOrDefault();
                if (session == null)
                {
                    session = new dtSession() { user = user.id, hostAddress = hostAddress};
                    db.dtSessions.Add(session);
                }
                session.expires = DateTime.Now.AddDays(7);
                session.session = sessionGuid.ToString();
                db.SaveChanges();
                int cookieCt = 0;
                foreach (var header in ctx.Response.Headers.Values)
                {
                    if (header.Count > 0)
                    {
                        foreach(var h in header)
                        {
                            foreach (var cookie in h.Split(";"))
                            {
                                if (cookie.Split("=").Length > 1 && cookie.Split("=")[0] == "dtSessionId") cookieCt = cookieCt + 1;
                            }
                        }
                    }
                }
                for (int i=0; i<cookieCt; i++) ctx.Response.Cookies.Delete("dtSessionId");
                ctx.Response.Cookies.Append("dtSessionId", sessionGuid.ToString(), new CookieOptions() { Expires = DateTime.Now.AddDays(7) });
                login.Session = sessionGuid.ToString();
            }
            return login;
        }
    }
}
