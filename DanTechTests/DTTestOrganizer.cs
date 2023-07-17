﻿using Microsoft.Extensions.Configuration;
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
        private static dgdb _db = null;
        private static Mock<IDTDPDAL> _dal = null;
        private static IDTDPDAL _dal_live = null;
        private static Mock<IDTGoogleAuthService> _google = null;

        public static int _numberOfProjects = 0;
        //public static dgdb DB(int numberOfProjects = 0) { if (_db == null) _db = DTDB.getDB(numberOfProjects); return _db; }
        public static IDTDPDAL DAL() { if (_dal == null) _dal = new Mock<IDTDPDAL>(); return _dal.Object; }
        public static IDTDPDAL DAL_LIVE() { if (_dal_live == null) _dal_live = new DTDPDAL(_db); return _dal_live; }
        public static IDTGoogleAuthService Google() { if (_google == null) _google = new Mock<IDTGoogleAuthService>(); return _google.Object; }
        public static string Conn = string.Empty;
        public static dtUser TestUser { get; set; }

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

            _dal_live = new DTDPDAL(_db);

            _dal = new Mock<IDTDPDAL>();
            _dal.Setup(x => x.testDatum(DTTestConstants.TestGoogleCodeTitle)).Returns(googleCodeDatum);

            var goodUser = _dal_live.user(new DGDAL_Email() { Email = DTTestConstants.TestKnownGoodUserEmail });
            var goodUserSession = _dal_live.session(goodUser.id);
            GoodGoogleTokens["AccessToken"] = goodUser.token;

            _google = new Mock<IDTGoogleAuthService>();

            _google.Setup(x => x.AuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(BadGoogleTokens);
            _google.Setup(x => x.AuthToken(DTTestConstants.TestGoogleCodeTitle, DTTestConstants.LocalHostDomain, It.IsAny<string>())).Returns(GoodGoogleTokens);
            _google.Setup(x => x.SetLogin(It.IsAny<Userinfo>(), It.IsAny<HttpContext>(), It.IsAny<IDTDPDAL>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new DanTech.Models.Data.dtLogin() { Email = goodUser.email, FName = goodUser.fName, LName = goodUser.lName, Session = goodUserSession.session });
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

        public static DTController InitializeDTController(dgdb db, bool withLoggedInUser, string userEmail = "", IDTDPDAL dal = null)
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
            var controller = new DTController(InitConfiguration(), logger, db, dal);

            return controller;
        }

        public static HomeController InitializeHomeController(IDTDPDAL dal, bool withLoggedInUser, string userEmail = "", string sessionId = "")
        { 
            string testHost = DTTestConstants.TestRemoteHost;        
            if (withLoggedInUser)
            {
                if (string.IsNullOrEmpty(userEmail)) userEmail = DTTestConstants.TestUserEmail;
                var user = dal.user(new DGDAL_Email() { Email = userEmail });
                var testSession = dal.session(user.id);
                if (testSession == null)
                {
                    testSession = new dtSession()
                    {
                        user = user.id,
                        hostAddress = DTTestConstants.TestRemoteHostAddress,
                        expires = DateTime.Now.AddDays(7),
                        session = DTTestConstants.TestSessionId
                    };
                    testHost = testSession.hostAddress;
                    dal.Add(testSession);
                }
                else
                {
                    testSession.expires = DateTime.Now.AddDays(7);
                    testSession.session = DTTestConstants.TestSessionId;
                }
            }

            var ctl = new HomeController(InitConfiguration(), new ServiceCollection().AddLogging().BuildServiceProvider().GetService<ILoggerFactory>().CreateLogger<HomeController>(), dal.GetDB(), dal);
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
            if (string.IsNullOrEmpty(sessionId)) sessionId = DTTestConstants.TestSessionId;
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

        public static void Cleanup(dgdb db)
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
