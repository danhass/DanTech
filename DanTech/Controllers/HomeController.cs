using DanTech.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using DanTech.Services;
using Microsoft.AspNetCore.Http;
using System.Web.Http.Cors;
using DanTech.Data;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace DanTech.Controllers
{
    public class HomeController : DTController
    {
        protected IDTGoogleAuthService _google = null;
        public HomeController(IConfiguration configuration, ILogger<HomeController> logger, IDTDBDataService data) : 
            base(configuration, logger, data)
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
            var typeCt = _db.Types.ToList().Count;
            ViewBag.ipAddress = HttpContext.Connection.RemoteIpAddress;
            ViewBag.host = Request.Host.Value;

            var v = VM;
            return View(VM);
        }
                
        [ServiceFilter(typeof(DTAuthenticate))]
        [DisableCors]
        public IActionResult GoogleSignin(string code)
        { 
            dtMisc testDatum = new dtMisc() { title = "Google Signin Code", value = code};
            _db.Log(testDatum);
            string domain = Request.Scheme + "://" + Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            var googleAuthService = new DTGoogleAuthService();
            googleAuthService.SetConfig(_configuration);
            var tokens = googleAuthService.AuthToken(code, domain, new List<string>() { DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleCalendarScope }, _configuration, "Home/GoogleSignin");
            var cred = GoogleCredential.FromAccessToken(tokens["AccessToken"], null);
            var oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer()
            {
                HttpClientInitializer = cred
            });
            var userInfo = oauthSerivce.Userinfo.Get().Execute();
            var login = _db.SetLogin(userInfo.Email, userInfo.GivenName, userInfo.FamilyName, domain, 1, tokens["AccessToken"], tokens["RefreshToken"]);
            SetVM(login.Session);
            Response.Cookies.Delete("dtSessionId");
            Response.Cookies.Append("dtSessionId", login.Session);  
            return View(VM);
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        [DisableCors]
        public IActionResult SaveGoogleCode(string code)
        {
            dtMisc? testDatum = _db.Misces.Where(x => x.title == "Google Signin Code").FirstOrDefault();
            if (testDatum == null) testDatum = new dtMisc() { title = "Google Signin Code", value = code };
            else testDatum.value = code;
            _db.Set(testDatum);
            dtUser? testUser = _db.Users.Where(x => x.email == DTControllerConstants.EmailUsedForTesting).FirstOrDefault();
            if (testUser != null)
            {
                testUser.token = "";
                testUser.refreshToken = "";
                _db.Set(testUser);
            }
            return RedirectToAction("Index", "Home");
        }

        [Microsoft.AspNetCore.Mvc.Route("/google")]
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
            googleAuthService.SetConfig(_configuration);
            var tokens = googleAuthService.AuthToken(code, domain, new List<string>() { DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleCalendarScope}, _configuration, "Home/GoogleSignin");
            if (!string.IsNullOrEmpty(tokens["AccessToken"]))
            {
                var cred = GoogleCredential.FromAccessToken(tokens["AccessToken"], null);
                var oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = cred
                });
                var userInfo = oauthSerivce.Userinfo.Get().Execute();
                login = _db.SetLogin(userInfo.Email, userInfo.GivenName, userInfo.FamilyName, domain, 1, tokens["AccessToken"], tokens["RefreshToken"]);
                SetVM(login.Session);
                Response.Cookies.Delete("dtSessionId");
                Response.Cookies.Append("dtSessionId", sessionId);
            }
            var json = Json(login);
            return json;
        }
        
        [Microsoft.AspNetCore.Mvc.Route("/login/cookie")]
        [DisableCors]
        public JsonResult EstablishSession()
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            string sessionId = Request.Cookies["dtSessionId"];
            string log = "";
            string addr = HttpContext.Request.Host.Value;
            dtLogin? login = null; 
            var session = _db.Sessions.Where(x => x.session == sessionId).FirstOrDefault();
            if (session != null)
            {
                var usr = _db.Users.Where(x => x.id == session.user).FirstOrDefault();
                if (usr != null)
                {
                    login = _db.SetLogin(usr.email, addr);
                }
            }
            var json = Json(login);
            return json;
        }

        [Microsoft.AspNetCore.Mvc.Route("/login")]
        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult Login(string sessionId)
        {
            //Response.Headers.Add("Access-Control-Allow-Origin", "*");
            string log = "";

            string addr = HttpContext.Request.Host.Value;
            dtSession? session = _db.Sessions.Where(x => x.session == sessionId).FirstOrDefault();
            var json = Json(session == null ? null : _db.SetLogin(session.userNavigation.email, addr));
            return json;
        } 
        
        [Microsoft.AspNetCore.Mvc.Route("/ping")]
        [ServiceFilter(typeof(DTAuthenticate))]
        public JsonResult Ping(string sessionId)
        {
            //Response.Headers.Add("Access-Control-Allow-Origin", "*");
            return Json(new { response = "api is alive", submitted = "sessionId: " + sessionId});
        }

        [DisableCors]
        public dtLogin EstablishSession(string authToken, string refreshToken)
        {
            string addr = HttpContext.Request.Host.Value;
            var google = new DTGoogleAuthService();
            google.SetConfig(_configuration);
            var userInfo = google.GetUserInfo(authToken, refreshToken);
            dtLogin login = _db.SetLogin(userInfo.Email, userInfo.GivenName, userInfo.FamilyName, addr, 1, authToken, refreshToken);
            if (!string.IsNullOrEmpty(login.Session))
            {
                HttpContext.Response.Cookies.Append("dtSession", login.Session);
            }
            SetVM(login.Session);
            return login;
        }

        [DisableCors]
        public IActionResult Signin()
        {
            string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            var google = new DTGoogleAuthService();
            google.SetConfig(_configuration);
            return Redirect(google.AuthService(domain, "Home/GoogleSignin", new List<string>() { DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleCalendarScope}));
        }

        [DisableCors]
        public IActionResult SigninForTest()
        {
            string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            var google = new DTGoogleAuthService();
            google.SetConfig(_configuration);
            return Redirect(google.AuthService(domain, "Home/SaveGoogleCode", new List<string>() { DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleCalendarScope } ));
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
                _db.ToggleTestFlag();
                string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
                var google = new DTGoogleAuthService();
                google.SetConfig(_configuration);
                return Redirect(google.AuthService(domain, "Home/GoogleSignin", new List<string>() { DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleCalendarScope }));
            }
            
            return RedirectToAction("Index");
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        public ViewResult SetupTests()
        {           
            _db.ClearResetFlags();
            return View(new DTViewModel() { StatusMessage = "Test set up complete." } );
        }
    }
}
