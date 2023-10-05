using Google.Apis.Oauth2.v2.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using DanTech.Models.Data;


namespace DanTech.Services
{
    public interface IDTGoogleAuthService
    {
        public string AuthService(string returnDomain, string returnHandler, IConfiguration config);
        public Dictionary<string, string> AuthToken(string code, string domain, string endPoint = "");
        public Userinfo GetUserInfo(string token, string refreshToken = "");
        public string RefreshAuthToken(string refreshToken);
        public dtLogin SetLogin(string sessionId, string hostAddress, IDTDBDataService db, ref string log);
        public dtLogin SetLogin(Userinfo userInfo, HttpContext ctx, IDTDBDataService db, string accessToken, string refreshToken);
    }
}
