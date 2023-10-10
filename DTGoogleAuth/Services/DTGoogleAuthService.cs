using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Oauth2.v2.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using DanTech.Data;
using Google.Apis.Services;
using DanTech.Data.Models;

namespace DanTech.Services
{
    public class DTGoogleAuthService : IDTGoogleAuthService
    {
        private static readonly string GoogleAuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private static readonly string GoogleCalendarScope = "https://www.googleapis.com/auth/calendar";
        private static readonly string GoogleUserInfoEmailScope = "https://www.googleapis.com/auth/userinfo.email";
        private static readonly string GoogleUserInfoProfileScope = "https://www.googleapis.com/auth/userinfo.profile";

        private IConfiguration? _config = null;

        public void SetConfig(IConfiguration config) { _config = config; }
 
        public string AuthService(string returnDomain, string returnHandler, List<string> scopes)
        {
            if (_config == null) throw new Exception("Must set config file with Google Info.");
            if (scopes == null || scopes.Count == 0) throw new Exception("Must have at least one scope for Google Auth");
            string r = GoogleAuthEndpoint + "?" +
                "scope=";
            for(int i = 0; i < scopes.Count; i++)
            {
                if (i > 0) r += " ";
                r += scopes[i];
            }
            r += "&state=google_signin" +
                "&redirect_uri=https://" + returnDomain + (returnHandler.StartsWith('/') ? "" : "/") + returnHandler +
                "&access_type=offline" +
                "&response_type=code" +
                "&client_id=" + _config.GetValue<string>("Google:ClientId");

            return r;
        }

        public string RefreshAuthToken(string refreshToken, List<string> scopes)
        {
            string tokenString = "";
            if (_config == null) throw new Exception("Must set config file with Google Info.");

            var clientSecrets = new ClientSecrets
            {
                ClientId = _config.GetValue<string>("Google:ClientId"),
                ClientSecret = _config.GetValue<string>("Google:ClientSecret")
            };
            var credential = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = clientSecrets,
                    Scopes = scopes.ToArray() 
                }
            ); ;
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

        public Dictionary<string, string> AuthToken(string code, string domain, List<string> scopes, string endPoint = "")
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
                    Scopes = scopes.ToArray()
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

                cred = GoogleCredential.FromAccessToken(RefreshAuthToken(refreshToken, new List<string>() { GoogleUserInfoEmailScope, GoogleUserInfoProfileScope, GoogleCalendarScope }));
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

        public dtLogin SetLogin(string sessionId, string hostAddress, IDTDBDataService db, ref string log)
        {
            dtLogin login = new dtLogin();
            db.RemoveOutOfDateSessions();
            var session = db.Sessions.Where(x => x.session == sessionId).FirstOrDefault();
            if (session != null)
            {
                login.Session = session.session;
                var user = db.Users.Where(x => x.id == session.user).FirstOrDefault();
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

        public dtLogin SetLogin(Userinfo userInfo, string hostAddress, IDTDBDataService db, string accessToken, string refreshToken)
        {
            dtLogin login = new dtLogin();
            Guid sessionGuid = Guid.NewGuid();
            if (userInfo != null && !string.IsNullOrEmpty(userInfo.Email) && !(string.IsNullOrEmpty(userInfo.GivenName) && string.IsNullOrEmpty(userInfo.FamilyName)))
            {
                var user = db.Users.Where(x => (x.email != null && x.email.ToLower() == userInfo.Email.ToLower())).FirstOrDefault();
                if (user == null)
                {
                    user = new dtUser() { type = 1 };
                }
                user.email = userInfo.Email;
                user.fName = userInfo.GivenName;
                user.lName = userInfo.FamilyName;
                user.lastLogin = DateTime.Now;
                user.token = accessToken;
                user.refreshToken = refreshToken;
                user = db.Set(user);
                login.Email = userInfo.Email;
                login.FName = userInfo.GivenName;
                login.LName = userInfo.FamilyName;

                var session = db.Sessions.Where(x => (x.user == user.id && x.hostAddress == hostAddress)).FirstOrDefault();
                if (session == null)
                {
                    session = new dtSession() { user = user.id, hostAddress = hostAddress};
                }
                session.session = sessionGuid.ToString();
                session = db.Set(session);
                login.Session = sessionGuid.ToString();
            }
            return login;
        }
    }
}
