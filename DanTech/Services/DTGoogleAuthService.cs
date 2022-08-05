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

namespace DanTech.Services
{
    public class DTGoogleAuthService
    {
        private static readonly string GoogleAuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private static readonly string GoogleCalendarScope = "https://www.googleapis.com/auth/calendar";
        private static readonly string GoogleUserInfoEmailScope = "https://www.googleapis.com/auth/userinfo.email";
        private static readonly string GoogleUserInfoProfileScope = "https://www.googleapis.com/auth/userinfo.profile";

        public static string AuthService (string returnDomain, string returnHandler, IConfiguration config)
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

        public static string RefreshAuthToken(string refreshToken)
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
            catch(Exception ex)
            {
                string msg = ex.Message;
            }
            return tokenString;
        }

        public static Dictionary<string, string> AuthToken(string code, string domain)
        {
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
                    "https://" + domain + "/Home/GoogleSignin",
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

        public static Userinfo GetUserInfo (string token, string refreshToken = "")
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

        public static string SetLogin(Userinfo userInfo, HttpContext ctx, dgdb db, string accessToken, string refreshToken)
        {
            Guid sessionGuid = Guid.NewGuid(); ;
            if (!string.IsNullOrEmpty(userInfo.Email) && !(string.IsNullOrEmpty(userInfo.GivenName) && string.IsNullOrEmpty(userInfo.FamilyName)))
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

                var ipAddress = ctx.Connection.RemoteIpAddress.ToString();
                var session = (from x in db.dtSessions where x.user == user.id && x.hostAddress == ipAddress select x).FirstOrDefault();
                if (session == null)
                {
                    var oldSession = (from x in db.dtSessions where x.user == user.id select x).FirstOrDefault();
                    if (oldSession!=null) // Must purge old session
                    {
                        db.dtSessions.Remove(oldSession);
                        db.SaveChanges();
                    }
                    session = new dtSession() { user = user.id, hostAddress = ipAddress};
                    db.dtSessions.Add(session);
                }
                session.expires = DateTime.Now.AddDays(1);
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
                ctx.Response.Cookies.Append("dtSessionId", sessionGuid.ToString());
            }
            else
            {
                return Guid.Empty.ToString();
            }
            return sessionGuid.ToString();
        }
    }
}
