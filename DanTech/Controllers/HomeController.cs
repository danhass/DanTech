﻿using DanTech.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DanTech.Data;
using Google.Apis.Auth.AspNetCore3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using WebMatrix.WebData;
using Google.Apis.PeopleService.v1;
using Google.Apis.Gmail.v1;
using System.Net.Http;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Oauth2.v2.Data;
using Microsoft.Extensions.Configuration;
using DanTech.Services;
using Microsoft.AspNetCore.Http;
using System.Net;

namespace DanTech.Controllers
{
 
    public class HomeController : DTController
    {
        public HomeController(IConfiguration configuration, ILogger<HomeController> logger, dgdb dgdb) : base(configuration, logger, dgdb)
        {
            
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public IActionResult Index()
        {
            var typeCt = (from x in _db.dtTypes where 1 == 1 select x).ToList().Count;
            ViewBag.ipAddress = HttpContext.Connection.RemoteIpAddress;

            var v = VM;
            return View(VM);
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public IActionResult GoogleSignin(string code)
        {            
            if (VM.TestEnvironment && DTDBDataService.SetIfTesting(_db, "Google code", code)) return RedirectToAction("SetupTests");
            string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            string accessToken = DTGoogleAuthService.AuthToken(code, domain, _configuration);
            var cred = GoogleCredential.FromAccessToken(accessToken, null);
            var oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer()
            {
                HttpClientInitializer = cred,
            });
            var userInfo = oauthSerivce.Userinfo.Get().ExecuteAsync().Result;
            string sessionId = DTGoogleAuthService.SetLogin(userInfo, HttpContext, _db, accessToken);
            SetVM(sessionId);
            Response.Cookies.Append("dtSessionId", sessionId);            

            return View(VM);
        } 

        public IActionResult Signin()
        {
            string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);            
            return Redirect(DTGoogleAuthService.AuthService(domain, "Home/GoogleSignin", _configuration));
        }
                
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public IActionResult ToggleTestState()
        {
            
            if (VM.TestEnvironment)
            {
                DTDBDataService dataService = new DTDBDataService(_db);
                dataService.ToggleTestFlag();
                string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
                return Redirect(DTGoogleAuthService.AuthService(domain, "Home/GoogleSignin", _configuration));
            }
            
            return RedirectToAction("Index");
        }

        public ViewResult SetupTests()
        {
            Dictionary<string, string> model = new Dictionary<string, string>();
            model["State"] = "Test data initialized";
            return View(model);
        }


    }
}
