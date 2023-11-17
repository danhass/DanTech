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
        public static readonly string GoogleAuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        public static readonly string GoogleCalendarScope = "https://www.googleapis.com/auth/calendar";
        public static readonly string GoogleUserInfoEmailScope = "https://www.googleapis.com/auth/userinfo.email";
        public static readonly string GoogleUserInfoProfileScope = "https://www.googleapis.com/auth/userinfo.profile";
        public static readonly string GoogleMailScope = "https://mail.google.com/";
        public static readonly string GoogleGmailSendScope = "https://www.googleapis.com/auth/gmail.send";
        public static readonly string GoogleGmailModifyScope = "https://www.googleapis.com/auth/gmail.modify";

        private IConfiguration? _config = null;

        public void SetConfig(IConfiguration config) { _config = config; }
 
        public string AuthService(string returnDomain, string returnHandler, List<string> scopes, bool gmail = false)
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
            string? clientId = gmail ? _config.GetValue<string>("Gmail:ClientId") : _config.GetValue<string>("Google:ClientId");
            r += "&state=google_signin" +
                "&redirect_uri=https://" + returnDomain + (returnHandler.StartsWith('/') ? "" : "/") + returnHandler +
                "&access_type=offline" +
                "&response_type=code" +
                "&client_id=" + clientId;

            return r;
        }

        public string RefreshAuthToken(string refreshToken, List<string> scopes, bool gmail = false)
        {
            string tokenString = "";
            if (_config == null) throw new Exception("Must set config file with Google Info.");

            var clientSecrets = new ClientSecrets
            {
                ClientId = gmail ? _config.GetValue<string>("Gmail:ClientId") : _config.GetValue<string>("Google:ClientId"),
                ClientSecret = gmail ? _config.GetValue<string>("Gmail:ClientSecret") : _config.GetValue<string>("Google:ClientSecret")
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

        public Dictionary<string, string> AuthToken(string code, string domain, List<string> scopes, IConfiguration config, string endPoint = "", bool gmail = false)
        {
            if (string.IsNullOrEmpty(endPoint)) endPoint = "/Home/GoogleSignin";
            if (!endPoint.StartsWith("/")) endPoint = "/" + endPoint;
            Dictionary<string, string> res = new Dictionary<string, string>();
            var clientSecrets = new ClientSecrets
            {
                ClientId = gmail ? _config!.GetValue<string>("Gmail:ClientId") : _config!.GetValue<string>("Google:ClientId"),
                ClientSecret = gmail ? _config!.GetValue<string>("Gmail:ClientSecret") : _config!.GetValue<string>("Google:ClientSecret")
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

        public Userinfo? GetUserInfo (string? token, string? refreshToken = "", bool gmail = false)
        {
            var cred = GoogleCredential.FromAccessToken(token, null);
            var oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer()
            {
                HttpClientInitializer = cred
            });
            Userinfo? userInfo = null;
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
                List<string> scopes = new List<string>() { GoogleUserInfoEmailScope, GoogleUserInfoProfileScope };
                if (gmail)
                {
                    scopes.Add(GoogleMailScope);
                    scopes.Add(GoogleGmailSendScope);
                    scopes.Add(GoogleGmailModifyScope);
                }
                else
                {
                    scopes.Add(GoogleCalendarScope);
                }
                cred = GoogleCredential.FromAccessToken(RefreshAuthToken(refreshToken, scopes));
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
    }
}
