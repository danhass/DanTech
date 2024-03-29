﻿using Google.Apis.Oauth2.v2.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using DanTech.Data.Models;


namespace DanTech.Services
{
    public interface IDTGoogleAuthService
    {
        public void SetConfig(IConfiguration config);
        public string AuthService(string returnDomain, string returnHandler, List<string> scopes, bool gmail = false);
        public Dictionary<string, string> AuthToken(string code, string domain, List<string> scopes, IConfiguration config, string endPoint = "", bool gmail = false);
        public Userinfo? GetUserInfo(string token, string refreshToken = "", bool gmail = false);
        public string RefreshAuthToken(string refreshToken, List<string> scopes, bool gmail = false);
    }
}
