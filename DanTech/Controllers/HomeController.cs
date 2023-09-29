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
using System.Web.Http.Cors;
using System.Collections.Specialized;
using DanTech.Models.Data;

namespace DanTech.Controllers
{
    public class HomeController : DTController
    {
        protected IDTGoogleAuthService _google = null;
        public HomeController(IConfiguration configuration, ILogger<HomeController> logger, dtdb dtdb) : 
            base(configuration, logger, dtdb)
        {
        }

        public void SetGoogle(IDTGoogleAuthService google)
        {
            _google = google;
        }
        public JsonResult SetCredentials(string token)
        {
            return Json(DTDBDataService.SetCredentials(token));
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public IActionResult Index()
        {
            // This method lets us use the web app kind of like a console app that is used a lot
            //   for ad hoc processing tasks, research, and experimentation.
            // We are leaving this here because it is used so often.
            //DTDBDataService svc = new DTDBDataService(_db, _configuration.GetConnectionString("dg"));
            //DTDBDataService.GeneralUtil(_db);
            var typeCt = (from x in _db.dtTypes where 1==1 select x).ToList().Count;
            ViewBag.ipAddress = HttpContext.Connection.RemoteIpAddress;
            ViewBag.host = Request.Host.Value;

            var v = VM;
            return View(VM);
        }
                
        [ServiceFilter(typeof(DTAuthenticate))]
        [DisableCors]
        public IActionResult GoogleSignin(string code)
        { 
            string domain = Request.Scheme + "://" + Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            var googleAuthService = new DTGoogleAuthService();
            var tokens = googleAuthService.AuthToken(code, domain);
            var cred = GoogleCredential.FromAccessToken(tokens["AccessToken"], null);
            var oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer()
            {
                HttpClientInitializer = cred
            });
            var userInfo = oauthSerivce.Userinfo.Get().Execute();
            var login = googleAuthService.SetLogin(userInfo, HttpContext, _svc, tokens["AccessToken"], tokens["RefreshToken"]);
            SetVM(login.Session);
            Response.Cookies.Delete("dtSessionId");
            Response.Cookies.Append("dtSessionId", login.Session);  
            return View(VM);
        } 

        [Route("/google")]
        [DisableCors]
        public JsonResult EstablishSession(string code, bool useCaller, string domain)
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            dtLogin login = new dtLogin();
            if (!useCaller || string.IsNullOrEmpty(domain))
            {
                domain = Request.Headers["protocol"] + "://" + Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            }
            string sessionId = "None";
            var googleAuthService = _google == null ? new DTGoogleAuthService() : _google;
            var tokens = googleAuthService.AuthToken(code, domain, "/google");
            if (!string.IsNullOrEmpty(tokens["AccessToken"]))
            {
                var cred = GoogleCredential.FromAccessToken(tokens["AccessToken"], null);
                var oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = cred
                });
                var userInfo = oauthSerivce.Userinfo.Get().Execute();
                login = googleAuthService.SetLogin(userInfo, HttpContext, _svc, tokens["AccessToken"], tokens["RefreshToken"]);
                SetVM(login.Session);
                Response.Cookies.Delete("dtSessionId");
                Response.Cookies.Append("dtSessionId", sessionId);
            }
            var json = Json(login);
            return json;
        }
        
        [Route("/login/cookie")]
        [DisableCors]
        public JsonResult EstablishSession()
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            string sessionId = Request.Cookies["dtSessionId"];
            string log = "";
            string addr = HttpContext.Request.Host.Value;
            var json = Json(new DTGoogleAuthService().SetLogin(sessionId, addr, _svc, ref log));
            return json;
        }

        [Route("/login")]
        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult Login(string sessionId)
        {
            //Response.Headers.Add("Access-Control-Allow-Origin", "*");
            string log = "";

            string addr = HttpContext.Request.Host.Value;
            var json = Json(new DTGoogleAuthService().SetLogin(sessionId, addr, _svc, ref log));
            return json;
        } 
        
        [Route("/ping")]
        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult Ping(string sessionId)
        {
            //Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return Json(new { response = "api is alive", submitted = "sessionId: " + sessionId});
        }

        [DisableCors]
        public dtLogin EstablishSession(string authToken, string refreshToken)
        {
            var google = new DTGoogleAuthService();
            dtLogin login = google.SetLogin(google.GetUserInfo(authToken, refreshToken), HttpContext, _svc, authToken, refreshToken);
            SetVM(login.Session);
            return login;
        }

        [DisableCors]
        public IActionResult Signin()
        {
            string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            var google = new DTGoogleAuthService();
            return Redirect(google.AuthService(domain, "Home/GoogleSignin", _configuration));
        }

        [DisableCors]
        public IActionResult Register(string email)
        {
            return Json(new { response = "Check your email. To complete registration click the link in the email." });
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
                _svc.ToggleTestFlag();
                string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
                var google = new DTGoogleAuthService();
                return Redirect(google.AuthService(domain, "Home/GoogleSignin", _configuration));
            }
            
            return RedirectToAction("Index");
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public ViewResult SetupTests()
        {           
            _svc.ClearResetFlags();
            return View(new DTViewModel() { StatusMessage = "Test set up complete." } );
        }
    }
}
