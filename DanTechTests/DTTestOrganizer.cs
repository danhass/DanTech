using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using DanTech.Controllers;
using Microsoft.Extensions.DependencyInjection;
using DanTech.Data;
using System.Net;
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

namespace DanTechTests
{
    [TestClass]
    public class DTTestOrganizer
    {
        private static dgdb _db = null;
        public static int _numberOfProjects = 0;
        public static dgdb DB(int numberOfProjects = 0) { if (_db == null) _db = DTDB.getDB(numberOfProjects); return _db; }

        [AssemblyInitialize()]
        public static void Init(TestContext context)
        {
            _db = DB(DTTestConstants.DefaultNumberOfTestPropjects);
            _numberOfProjects = DTTestConstants.DefaultNumberOfTestPropjects;
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

        public static DTController InitializeDTController(dgdb db, bool withLoggedInUser, string userEmail = "")
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
                testSession.session = DTTestConstants.TestSessionId;
                db.SaveChanges();
            }

            //Set up controller
            var logger = DTTestOrganizer.InitLogger();
            var controller = new DTController(InitConfiguration(), logger, db);

            return controller;
        }

        public static HomeController InitializeHomeController(dgdb db, bool withLoggedInUser, string userEmail = "", string sessionId = "")
        {
            if (withLoggedInUser)
            {
                if (string.IsNullOrEmpty(userEmail)) userEmail = DTTestConstants.TestUserEmail;
                var user = (from x in db.dtUsers where x.email == userEmail select x).FirstOrDefault();
                var testSession = (from x in db.dtSessions where x.user == user.id && x.hostAddress == DTTestConstants.TestRemoteHost select x).FirstOrDefault();
                if (testSession == null)
                {
                    testSession = new dtSession() { user = user.id, hostAddress = DTTestConstants.TestRemoteHostAddress };
                    db.dtSessions.Add(testSession);
                }
                testSession.expires = DateTime.Now.AddDays(7);
                testSession.session = DTTestConstants.TestSessionId;
                db.SaveChanges();
            }

            var ctl = new HomeController(InitConfiguration(), new ServiceCollection().AddLogging().BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger<HomeController>(), db);
            ctl.ControllerContext = new ControllerContext(new ActionContext(InitializeContext(DTTestConstants.TestRemoteHost, false, sessionId), new RouteData(), new ControllerActionDescriptor()));
            return ctl;
        }

        public static DefaultHttpContext InitializeContext(string host, bool withLoggedInUser, string sessionId = "", bool noCookie = false)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(host);
            var requestFeature = new HttpRequestFeature();
            var featureCollection = new FeatureCollection();
            requestFeature.Headers = new HeaderDictionary();
            if (string.IsNullOrEmpty(sessionId)) sessionId = DTTestConstants.TestSessionId;
            if (withLoggedInUser || !string.IsNullOrEmpty(sessionId) && !noCookie)
            {
                requestFeature.Headers.Add(HeaderNames.Cookie, new StringValues(DTTestConstants.SessionCookieKey + "=" + sessionId));
            }
            featureCollection.Set<IHttpRequestFeature>(requestFeature);
            var cookiesFeature = new RequestCookiesFeature(featureCollection);
            httpContext.Request.Cookies = cookiesFeature.Cookies;
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(DTTestConstants.TestRemoteHostAddress);
            return httpContext;
        }

        public static void Cleanup(dgdb db)
        {
            var u = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            if (u != null)
            {
                var projs = (from x in db.dtProjects where x.user == u.id select x).ToList();
                db.dtProjects.RemoveRange(projs);
                var sess = (from x in db.dtSessions where x.user == u.id select x).ToList();
                db.dtSessions.RemoveRange(sess);
                var planItems = (from x in db.dtPlanItems where x.user == u.id select x).ToList();
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
