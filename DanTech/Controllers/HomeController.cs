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
using EASendMail;
using System.Threading.Tasks;
using System;
using System.Web;
using Activity = System.Diagnostics.Activity;

namespace DanTech.Controllers
{
    public class HomeController : DTController
    {
        protected IDTGoogleAuthService _google = null;
        public HomeController(IConfiguration configuration, ILogger<HomeController> logger, IDTDBDataService data, dtdb dbctx) : 
            base(configuration, logger, data, dbctx)
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
        /*
        [ServiceFilter(typeof(DTAuthenticate))]
        [DisableCors]
        public async void GitHubSignin(string code)
        {
            string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            string rUrl = "https://" + domain + "/Home/GitHubSignin";
            var client = new HttpClient();
            string clientId = _configuration.GetValue<string>("GitHub:ClientId");
            string clientSecret = _configuration.GetValue<string>("GitHub:ClientSecret");
            var parameters = new Dictionary<string, string>
            {
                {"client_id", clientId },
                {"client_secret", clientSecret },
                {"code", code },
                {"redirect_url", rUrl }
            };
            var content = new FormUrlEncodedContent(parameters);
            var response = await client.PostAsync("https://github.com/login/oauth/access_token", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var values = HttpUtility.ParseQueryString(responseContent);
            var access_token = values["access_token"];
            var client1 = new GitHubClient(new Octokit.ProductHeaderValue("DanTech"));
            var tokenAuth = new Credentials(access_token);
            client1.Credentials = tokenAuth;
            var user = await client1.User.Current();
            var email = user.Email;
            RedirectToAction("Index", "Home");
            return;
        }
        */
        [ServiceFilter(typeof(DTAuthenticate))]
        [DisableCors]
        public IActionResult GoogleSignin(string code)
        {
            //dtMisc testDatum = new dtMisc() { title = "Google Signin Code", value = code};
            //_db.Log(testDatum);
            string hostAddress = HttpContext.Connection.RemoteIpAddress.ToString();
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
            var login = _db.SetLogin(userInfo.Email, userInfo.GivenName, userInfo.FamilyName, hostAddress, 1, tokens["AccessToken"], tokens["RefreshToken"]);
            SetVM(login.Session);
            Response.Cookies.Delete("dtSessionId");
            Response.Cookies.Append("dtSessionId", login.Session);  
            return View(VM);
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        [DisableCors]
        public IActionResult GoogleGmailSignin(string code)
        {
            dtMisc testDatum = new dtMisc() { title = "Google GMail Signin Code", value = code };
            _db.Log(testDatum);
            string domain = Request.Scheme + "://" + Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            var googleAuthService = new DTGoogleAuthService();
            googleAuthService.SetConfig(_configuration);
            var tokens = googleAuthService.AuthToken(code, domain, new List<string>() { DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleCalendarScope, DTGoogleAuthService.GoogleMailScope, DTGoogleAuthService.GoogleGmailSendScope, DTGoogleAuthService.GoogleGmailModifyScope }, _configuration, "Home/GoogleGmailSignin", true);
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
            return RedirectToAction("Index", "Home");
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        [DisableCors]
        public IActionResult SaveGoogleCode(string code)
        {
            dtMisc testDatum = _db.Misces.Where(x => x.title == "Google Signin Code").FirstOrDefault();
            if (testDatum == null) testDatum = new dtMisc() { title = "Google Signin Code", value = code };
            else testDatum.value = code;
            _db.Set(testDatum);
            dtUser testUser = _db.Users.Where(x => x.email == DTControllerConstants.EmailUsedForTesting).FirstOrDefault();
            if (testUser != null)
            {
                testUser.token = "";
                testUser.refreshToken = "";
                _db.Set(testUser);
            }
            return RedirectToAction("Index", "Home");
        }

        [ServiceFilter(typeof(DTAuthenticate))]
        [DisableCors]
        public IActionResult TestGmail()
        {
            ViewBag.Message = "Sending test email";

            ViewBag.Email = _configuration.GetValue<string>("Gmail:Email");

            var User = _db.Users.Where(x => x.email == ViewBag.Email).FirstOrDefault();
            ViewBag.Token = User != null ? User.token : "Not found";

            string Result = "";
            try
            {
                var gmailSvc = new DTGmailService();
                gmailSvc.SetConfig(_configuration);
                gmailSvc.SetAuthToken(User.token);
                gmailSvc.SetRefreshToken(User.refreshToken);
                gmailSvc.SetMailMessage("TryIt", User.email, new List<string>() { "hass.dan@gmail.com" }, "Test email from DTGmailService", "", "<b>Test</b> body (html)", new List<string>() { @"C:\Users\hassd\Documents\AT&T.pdf", @"C:\Users\hassd\Documents\WF.0723.pdf" });
                var sent = gmailSvc.Send();
                if (gmailSvc.GetAuthToken() != User.token) 
                {
                    User.token = gmailSvc.GetAuthToken();
                    _db.Set(User);
                }
                Result = sent ? "Email was sent successfully!" : "Email was not confirmed";
            }
            catch (Exception ep)
            {
                Result = String.Format("Failed to send email with the following error: {0}", ep.Message);
            }
            ViewBag.Result = Result;
            var v = VM;
            return View(VM);
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
            var hostAddress = this.HttpContext.Request.Host.Value;

            var googleAuthService = _google == null ? new DTGoogleAuthService() : _google;
            googleAuthService.SetConfig(_configuration);
            var tokens = googleAuthService.AuthToken(code, domain, new List<string>() { DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleCalendarScope}, _configuration, "/google");
            if (!string.IsNullOrEmpty(tokens["AccessToken"]))
            {
                var cred = GoogleCredential.FromAccessToken(tokens["AccessToken"], null);
                var oauthSerivce = new Google.Apis.Oauth2.v2.Oauth2Service(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = cred
                });
                var userInfo = oauthSerivce.Userinfo.Get().Execute();
                login = _db.SetLogin(userInfo.Email, userInfo.GivenName, userInfo.FamilyName, hostAddress, 1, tokens["AccessToken"], tokens["RefreshToken"]);
                SetVM(login.Session);
                Response.Cookies.Delete("dtSessionId");
                Response.Cookies.Append("dtSessionId", login.Session);
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
            string addr = HttpContext.Request.Host.Value;
            dtLogin login = null; 
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

            string addr = HttpContext.Connection.RemoteIpAddress.ToString();
            dtSession session = _db.Sessions.Where(x => x.session == sessionId).FirstOrDefault();
            dtUser user = null;
            if (session != null)
            {
                user = _db.Users.Where(x => x.id == session.user).FirstOrDefault();
            }
            var json = Json(user == null ? null : _db.SetLogin(user.email, addr));
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
        public IActionResult SignInForGitHub()
        {
            string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            var url = "https://github.com/login/oauth/authorize?client_id=" + _configuration.GetValue<string>("GitHub:ClientId") + "&scope=user";
            return Redirect(url);
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
        public IActionResult SigninForGmail()
        {
            string domain = Request.Headers["host"] + (string.IsNullOrEmpty(Request.Headers["port"]) ? "" : ":" + Request.Headers["port"]);
            var google = new DTGoogleAuthService();
            google.SetConfig(_configuration);
            return Redirect(google.AuthService(domain, "Home/GoogleGmailSignin", new List<string>() { DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleCalendarScope, DTGoogleAuthService.GoogleMailScope, DTGoogleAuthService.GoogleGmailSendScope, DTGoogleAuthService.GoogleGmailModifyScope }, true));
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
