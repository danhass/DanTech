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

        public static string AuthToken(string code, string domain, IConfiguration config)
        {
            string tokenString = "";
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

                tokenString = token.AccessToken;               
            }
            catch(Exception ex)
            {
                string msgs = ex.Message;
            }

            return tokenString;
        }

        public static string SetLogin(Userinfo userInfo, HttpContext ctx, dgdb db, string accessToken)
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
                ctx.Response.Cookies.Delete("dtSessionId");
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
