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
using DanTechTests.Data;
using DanTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Google.Apis.Oauth2.v2.Data;
using Moq;

namespace DanTechTests
{
    [TestClass]
    public class DTTestOrganizer
    {
        private static dtdb _db = null;
        private static Mock<IDTGoogleAuthService> _google = null;

        public static int _numberOfProjects = 0;
        //public static dtdb DB(int numberOfProjects = 0) { if (_db == null) _db = DTDB.getDB(numberOfProjects); return _db; }
        public static IDTGoogleAuthService Google() { if (_google == null) _google = new Mock<IDTGoogleAuthService>(); return _google.Object; }
        public static string Conn = string.Empty;
        public static dtUser TestUser { get; set; }
        public static dtSession TestSession { get; set; }

        public static Dictionary<string, string> BadGoogleTokens = new Dictionary<string, string>() { { "AccessToken", "" }, { "RefreshToken", "" } };
        public static Dictionary<string, string> GoodGoogleTokens = new Dictionary<string, string>() { { "AccessToken", DTTestConstants.TestValue }, { "RefreshToken", DTTestConstants.TestValue2 } };
                
        public static dtTestDatum googleCodeDatum = new dtTestDatum() { id = 1, title = DTTestConstants.TestGoogleCodeTitle, value = DTTestConstants.TestGoogleCodeTitle };

        [AssemblyInitialize()]
        public static void Init(TestContext context)
        {
            Conn = DTDB.Conn();
            _db = DTDB.getDB(DTTestConstants.DefaultNumberOfTestPropjects);
            var testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            if (testUser == null)
            {
                testUser = new dtUser() { type = 1, fName = DTTestConstants.TestUserFName, lName = DTTestConstants.TestUserLName, email = DTTestConstants.TestUserEmail };
                _db.dtUsers.Add(testUser);
                _db.SaveChanges();
            }
            TestUser = testUser;

            var goodUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();
            
            GoodGoogleTokens["AccessToken"] = goodUser.token;

            var goodUserSession = (from x in _db.dtSessions where x.user == goodUser.id && x.hostAddress == DTTestConstants.TestRemoteHost select x).FirstOrDefault();

            if (goodUserSession == null)
            {
                goodUserSession = new dtSession() { session = Guid.NewGuid().ToString(), user = goodUser.id, expires = DateTime.Now.AddDays(7), hostAddress = DTTestConstants.TestRemoteHost };
                _db.dtSessions.Add(goodUserSession);
                _db.SaveChanges();
            }

            _google = new Mock<IDTGoogleAuthService>();

            _google.Setup(x => x.AuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(BadGoogleTokens);
            _google.Setup(x => x.AuthToken(DTTestConstants.TestGoogleCode, DTTestConstants.LocalHostDomain, It.IsAny<string>())).Returns(GoodGoogleTokens);
            _google.Setup(x => x.SetLogin(It.IsAny<Userinfo>(), It.IsAny<HttpContext>(), It.IsAny<dtdb>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new DanTech.Models.Data.dtLogin() { Email = goodUser.email, FName = goodUser.fName, LName = goodUser.lName, Session = goodUserSession.session });

            TestSession = goodUserSession;
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            Cleanup(_db);
        }

        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json")
               .AddEnvironmentVariables()
               .Build();
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

        public static DTController InitializeDTController(dtdb db, bool withLoggedInUser, string userEmail = "")
        {
            //Set up db
            if (withLoggedInUser)
            {
                if (string.IsNullOrEmpty(userEmail)) userEmail = DTTestConstants.TestUserEmail;
                var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
                var testSession = (from x in db.dtSessions where x.user == testUser.id && x.hostAddress == DTTestConstants.TestRemoteHost select x).FirstOrDefault();
                if (testSession == null)
                {
                    testSession = new dtSession() { user = testUser.id, hostAddress = DTTestConstants.TestRemoteHost };
                    db.dtSessions.Add(testSession);
                }
                testSession.expires = DateTime.Now.AddDays(7);
                testSession.session = DTTestOrganizer.TestSession.session;
                db.SaveChanges();
            }

            //Set up controller
            var logger = DTTestOrganizer.InitLogger();
            var controller = new DTController(InitConfiguration(), logger, db);

            return controller;
        }

        public static HomeController InitializeHomeController(dtdb db, bool withLoggedInUser, string userEmail = "", string sessionId = "")
        { 
            string testHost = DTTestConstants.TestRemoteHost;        
            if (withLoggedInUser)
            {
                if (string.IsNullOrEmpty(userEmail)) userEmail = DTTestConstants.TestUserEmail;
                var user = (from x in db.dtUsers where x.email == userEmail select x).FirstOrDefault(); ;
                var testSession = (from x in db.dtSessions where x.user == user.id select x).FirstOrDefault();
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
                    db.dtSessions.Add(testSession);
                }
                else
                {
                    testSession.expires = DateTime.Now.AddDays(7);
                    testSession.session = DTTestOrganizer.TestSession.session;
                }
                db.SaveChanges();
            }

            var ctl = new HomeController(InitConfiguration(), new ServiceCollection().AddLogging().BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger<HomeController>(), db);
            ctl.ControllerContext = new ControllerContext(new ActionContext(InitializeContext(DTTestConstants.TestRemoteHost, false, sessionId), new RouteData(), new ControllerActionDescriptor()));
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

        public static void Cleanup(dtdb db)
        {
            var u = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            if (u != null)
            {
                var projs = (from x in db.dtProjects where x.user == u.id select x).ToList();
                db.dtProjects.RemoveRange(projs);
                var sess = (from x in db.dtSessions where x.user == u.id select x).ToList();
                db.dtSessions.RemoveRange(sess);
                var planItems = (from x in db.dtPlanItems where x.user == u.id && x.parent != null select x).ToList();
                db.dtPlanItems.RemoveRange(planItems);
                db.SaveChanges();
                planItems = (from x in db.dtPlanItems where x.user == u.id select x).ToList();
                db.dtPlanItems.RemoveRange(planItems);
                db.dtUsers.Remove(u);
                db.SaveChanges();
                var testData = (from x in _db.dtTestData where x.title != DTConstants.AuthTokensNeedToBeResetKey select x).ToList();
                db.dtTestData.RemoveRange(testData);
                db.SaveChanges();
            }
        }
         
        [TestMethod]
        public void DTTestOrganizer_valid()
        {
            Assert.IsNotNull(_db);
        }
    }
}
