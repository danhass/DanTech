using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using DanTech.Controllers;
using Microsoft.Extensions.DependencyInjection;
using DanTech.Data;
using System.Net;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using DanTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Google.Apis.Oauth2.v2.Data;
using Moq;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace DanTechTests
{
    [TestClass]
    public class DTTestOrganizer
    {
        private static IDTDBDataService _db = null;
        private static Mock<IDTGoogleAuthService> _google = null;
        private static IConfiguration _config = null;

        public static int _numberOfProjects = 0;
        //public static dtdb DB(int numberOfProjects = 0) { if (_db == null) _db = DTDB.getDB(numberOfProjects); return _db; }
        public static IDTGoogleAuthService Google() { if (_google == null) _google = new Mock<IDTGoogleAuthService>(); return _google.Object; }
        public static string Conn = string.Empty;
        public static dtUser GoodUser = null;
        public static dtUser TestUser = null;
        public static dtSession GoodUserSession = null;
        public static dtSession TestUserSession = null;
        public static dtSession TestSession = null;
        public static List<dtSession> Sessions = new List<dtSession>();

        public static Dictionary<string, string> BadGoogleTokens = new Dictionary<string, string>() { { "AccessToken", "" }, { "RefreshToken", "" } };
        public static Dictionary<string, string> GoodGoogleTokens = new Dictionary<string, string>() { { "AccessToken", DTTestConstants.TestValue }, { "RefreshToken", DTTestConstants.TestValue2 } };
                
        public static dtTestDatum googleCodeDatum = new dtTestDatum() { id = 1, title = DTTestConstants.TestGoogleCodeTitle, value = DTTestConstants.TestGoogleCodeTitle };

        [AssemblyInitialize()]
        public static void Init(TestContext context)
        {
            if (_config == null) _config = InitConfiguration();
            Conn = _config.GetConnectionString("DG");

            if (_db == null) _db = DataService();
            if (TestUser == null)
            {
                var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
                if (testUser == null)
                {
                    testUser = new dtUser() { type = 1, fName = DTTestConstants.TestUserFName, lName = DTTestConstants.TestUserLName, email = DTTestConstants.TestUserEmail };
                    testUser = _db.Set(testUser);
                }
                TestUser = testUser;
            }
            if (TestUserSession == null && TestUser != null)
            {
                TestUserSession = _db.Sessions.Where(x => x.user == TestUser.id).FirstOrDefault();
                if (TestUserSession == null)
                {
                    var session = new dtSession() { session = Guid.NewGuid().ToString(), user = TestUser.id, expires = DateTime.Now.AddDays(7), hostAddress = DTTestConstants.TestRemoteHost };
                    TestUserSession = _db.Set(session);
                }
            }
            SetGoodUserData();
            Sessions = _db.Sessions;
  
        }
        public static void SetGoodUserData()
        {
            if (GoodUser == null)
            {
                GoodUser = _db.Users.Where(x => x.email == DTTestConstants.TestKnownGoodUserEmail).FirstOrDefault();
            }
            GoodGoogleTokens["AccessToken"] = GoodUser != null ? GoodUser.token: string.Empty;
            if (GoodUserSession == null && GoodUser != null)
            {
                GoodUserSession = _db.Sessions.Where(x => x.user == GoodUser.id && x.hostAddress == DTTestConstants.LocalHostDomain).FirstOrDefault();
                if (GoodUserSession == null)
                {
                    GoodUserSession = new dtSession() { session = Guid.NewGuid().ToString(), user = GoodUser.id, expires = DateTime.Now.AddDays(7), hostAddress = DTTestConstants.LocalHostDomain };
                    GoodUserSession = _db.Set(GoodUserSession);
                }
            }

            _google = new Mock<IDTGoogleAuthService>();

            _google.Setup(x => x.AuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<IConfiguration>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(BadGoogleTokens);
            _google.Setup(x => x.AuthToken(DTTestConstants.TestGoogleCode, DTTestConstants.LocalHostDomain, It.IsAny<List<string>>(), It.IsAny<IConfiguration>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(GoodGoogleTokens);
            
            TestSession = GoodUserSession;
        }
        [AssemblyCleanup]
        public static void Cleanup()
        {
            Cleanup(_db);
        }
        public static IConfiguration InitConfiguration()
        {
            var bldr = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json");
            var config = bldr.Build();
            return config;
        }

        public static ILogger<DTController> InitLogger()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();

            var logger = factory.CreateLogger<DTController>();
            return logger;
        }

        public static IDTDBDataService DataService()
        {
            if (_db == null)
            {
                if (_config == null) InitConfiguration();
                _db = new DTDBDataService(_config.GetConnectionString("DG"));
            }
            return _db;
        }

        public static DTController InitializeDTController(bool withLoggedInUser, dtUser testUser = null)
        {
            var cfg = InitConfiguration();
            var dbctx = new dtdb(cfg.GetConnectionString("DG"));
            var db = new DTDBDataService(cfg, dbctx);
            //Set up db
            if (withLoggedInUser)
            {
                if (testUser == null)
                {
                    testUser = DTTestOrganizer.GoodUser;
                }
                var testSession = (from x in dbctx.dtSessions where x.user == testUser.id && x.hostAddress == DTTestConstants.LocalHostDomain select x).FirstOrDefault();
                if (testSession == null)
                {
                    testSession = GoodUserSession;
                }
                testSession.expires = DateTime.Now.AddDays(7);
                testSession.session = DTTestOrganizer.TestSession.session;
            }

            //Set up controller
            var logger = DTTestOrganizer.InitLogger();

            var controller = new DTController(cfg, logger, db, dbctx);

            return controller;
        }

        public static HomeController InitializeHomeController(bool withLoggedInUser, string userEmail = "", string sessionId = "")
        {
            var cfg = InitConfiguration();
            var dbctx = new dtdb(cfg.GetConnectionString("DG"));
            var db = new DTDBDataService(cfg, dbctx);
            string testHost = DTTestConstants.LocalHostDomain;        
            if (withLoggedInUser)
            {
                if (string.IsNullOrEmpty(userEmail)) userEmail = DTTestConstants.TestUserEmail;
                var user = db.Users.Where(x => x.email == userEmail).FirstOrDefault();
                var testSession = db.Sessions.Where(x => x.user == user.id).FirstOrDefault();
                if (testSession == null)
                {
                    testSession = new dtSession()
                    {
                        user = user.id,
                        hostAddress = DTTestConstants.TestRemoteHostAddress,
                        expires = DateTime.Now.AddDays(7),
                        session = DTTestOrganizer.TestSession.session
                    };
                    testHost = testSession.hostAddress;
                }
                else
                {
                    testSession.expires = DateTime.Now.AddDays(7);
                    testSession.session = DTTestOrganizer.TestSession.session;
                }
                db.Set(testSession);
            }

            var ctl = new HomeController(InitConfiguration(), new ServiceCollection().AddLogging().BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger<HomeController>(), db, dbctx);
            ctl.ControllerContext = new ControllerContext(new ActionContext(InitializeContext(DTTestConstants.LocalHostDomain, false, sessionId), new RouteData(), new ControllerActionDescriptor()));
            return ctl;
        }

        public static DefaultHttpContext InitializeContext(string host, bool withLoggedInUser, string sessionId = "", bool noCookie = false, string ipAddress = "")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(host);
            var requestFeature = new HttpRequestFeature();
            var featureCollection = new FeatureCollection();
            requestFeature.Headers = new HeaderDictionary();
            if (string.IsNullOrEmpty(sessionId)) sessionId = DTTestOrganizer.TestSession.session;
            if (withLoggedInUser || !string.IsNullOrEmpty(sessionId) && !noCookie)
            {
                requestFeature.Headers.Add(HeaderNames.Cookie, new StringValues(DTTestConstants.SessionCookieKey + "=" + sessionId));
            }
            featureCollection.Set<IHttpRequestFeature>(requestFeature);
            var cookiesFeature = new RequestCookiesFeature(featureCollection);
            httpContext.Request.Cookies = cookiesFeature.Cookies;
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(string.IsNullOrEmpty(ipAddress) ? DTTestConstants.TestRemoteHostAddress : ipAddress);
            return httpContext;
        }

        public static void Cleanup(IDTDBDataService db)
        {
            var u = db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            if (u != null)
            {
                db.Delete(db.Projects.Where(x => x.user == u.id).ToList());
                db.Delete(db.PlanItems.Where(x => x.user == u.id && x.parent != null).ToList());
                db.Delete(db.PlanItems.Where(x => x.user == u.id).ToList());
                db.Delete(db.Sessions.Where(x => x.user == u.id).ToList());
                db.Delete(u);
                db.Delete(db.TestData.Where(x => x.title != DTTestConstants.AuthTokensNeedToBeResetKey).ToList());
            }
            db.Delete(db.Sessions.Where(x => x.hostAddress == DTTestConstants.TestRemoteHost).ToList());
        }
         
        [TestMethod]
        public void DTTestOrganizer_valid()
        {
            Assert.IsNotNull(_db);
        }
    }
}
