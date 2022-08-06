using DanTech.Models;
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

namespace DanTech.Controllers
{

    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class HomeController : DTController
    {
        public HomeController(IConfiguration configuration, ILogger<HomeController> logger, dgdb dgdb) : base(configuration, logger, dgdb)
        {
            
        }

        public JsonResult SetCredentials(string token)
        {
            return Json(DTDBDataService.SetCredentials(token));
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
            string domain = Request.Scheme + "://" + Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            var tokens = DTGoogleAuthService.AuthToken(code, domain);
            var cred = GoogleCredential.FromAccessToken(tokens["AccessToken"], null);
            var oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer()
            {
                HttpClientInitializer = cred
            });
            var userInfo = oauthSerivce.Userinfo.Get().Execute();
            string sessionId = DTGoogleAuthService.SetLogin(userInfo, HttpContext, _db, tokens["AccessToken"], tokens["RefreshToken"]);
            SetVM(sessionId);
            Response.Cookies.Delete("dtSessionId");
            Response.Cookies.Append("dtSessionId", sessionId);  
            return View(VM);
        } 

        [DisableCors]
        [Route("/google")]                
        public JsonResult EstablishSession(string code, bool useCaller, string domain)
        {
            if (!useCaller || string.IsNullOrEmpty(domain))
            {
                domain = Request.Headers["protocol"] + "://" + Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            }
            string sessionId = "None";
            var tokens = DTGoogleAuthService.AuthToken(code, domain, "/google");
            if (!string.IsNullOrEmpty(tokens["AccessToken"]))
            {
                var cred = GoogleCredential.FromAccessToken(tokens["AccessToken"], null);
                var oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = cred
                });
                var userInfo = oauthSerivce.Userinfo.Get().Execute();
                sessionId = DTGoogleAuthService.SetLogin(userInfo, HttpContext, _db, tokens["AccessToken"], tokens["RefreshToken"]);
                SetVM(sessionId);
                Response.Cookies.Delete("dtSessionId");
                Response.Cookies.Append("dtSessionId", sessionId);
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
            }
            var json = Json(new { sessionId=sessionId });
            return json;
            }

            public string EstablishSession(string authToken, string refreshToken)
        {
            string sessionId = DTGoogleAuthService.SetLogin(DTGoogleAuthService.GetUserInfo(authToken, refreshToken), HttpContext, _db, authToken, refreshToken);
            SetVM(sessionId);
            return sessionId;
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

        [ServiceFilter(typeof(DTAuthenticate))]
        public ViewResult SetupTests()
        {
            DTDBDataService.ClearResetFlags();
            return View(new DTViewModel() { StatusMessage = "Test set up complete." } );
        }


    }
}
