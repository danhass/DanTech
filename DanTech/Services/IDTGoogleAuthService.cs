using Google.Apis.Oauth2.v2.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DanTech.Data;
using DanTech.Models.Data;


namespace DanTech.Services
{
    public interface IDTGoogleAuthService
    {
        public string AuthService(string returnDomain, string returnHandler, IConfiguration config);
        public Dictionary<string, string> AuthToken(string code, string domain, string endPoint = "");
        public Userinfo GetUserInfo(string token, string refreshToken = "");
        public string RefreshAuthToken(string refreshToken);
        public dtLogin SetLogin(string sessionId, string hostAddress, dtdb db, ref string log);
        public dtLogin SetLogin(Userinfo userInfo, HttpContext ctx, dtdb db, string accessToken, string refreshToken);
    }
}
