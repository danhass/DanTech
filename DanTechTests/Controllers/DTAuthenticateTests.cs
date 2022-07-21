using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DanTech.Controllers;
using Microsoft.Extensions.Configuration;
using DanTechTests.Data;
using DanTech.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;

namespace DanTechTests
{
    [TestClass]
    public class DTAuthenticateTests
    {
        private const string _sessionCookieKey = "dtSessionId";
        private const string _sessionGuid = "01ddb399-850b-4111-a3e6-6afd1c30b605";
        private const string _testUserEmail = "t@t.com"; //Permanent user in the user table that is reserved for testing.

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

        [TestMethod]
        public void ActionExecutingTest_UserLoggedIn()
        {
            //Arrange

            //Set up db
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == _testUserEmail select x).FirstOrDefault();
            var testSession = (from x in db.dtSessions where x.user == testUser.id select x).FirstOrDefault();
            if (testSession == null)
            {
                testSession = new dtSession() { user = testUser.id, hostAddress = IPAddress.Loopback.ToString() };
                db.dtSessions.Add(testSession);
            }
            testSession.expires = DateTime.Now.AddDays(1);
            testSession.session = _sessionGuid;
            db.SaveChanges();

            //Set up context
            var httpContext = new DefaultHttpContext();
            var requestFeature = new HttpRequestFeature();
            var featureCollection = new FeatureCollection();
            requestFeature.Headers = new HeaderDictionary();
            requestFeature.Headers.Add(HeaderNames.Cookie, new StringValues(_sessionCookieKey + "=" + _sessionGuid));
            featureCollection.Set<IHttpRequestFeature>(requestFeature);
            var cookiesFeature = new RequestCookiesFeature(featureCollection);
            httpContext.Request.Cookies = cookiesFeature.Cookies;
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

            //Set up controller
            var config = InitConfiguration();
            var logger = InitLogger();
            var controller = new DTController(config, logger, db);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            var actionFilter = new DTAuthenticate(config, db);

            //Act
            actionFilter.OnActionExecuting(ctx);

            //Clean up db - test user is left in place
            db.dtSessions.Remove(testSession);
            db.SaveChanges();

            //Assert
            Assert.IsNotNull(controller.VM, "Controller's VM not set.");
            Assert.IsNotNull(controller.VM.User, "User not set on VM.");
            Assert.AreEqual(controller.VM.User.email, _testUserEmail, "User set incorrectly");
        }

        [TestMethod]
        public void ActionExecutingTest_UserNotLoggedIn()
        {
            //Arrange

            //Set up db
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == _testUserEmail select x).FirstOrDefault();
            //Remove any session for this user
            var testSession = (from x in db.dtSessions where x.user == testUser.id select x).FirstOrDefault();
            if (testSession != null)
            {
                db.dtSessions.Remove(testSession);
                db.SaveChanges();
            }

            //Set up context
            var httpContext = new DefaultHttpContext();
            var requestFeature = new HttpRequestFeature();
            var featureCollection = new FeatureCollection();
            requestFeature.Headers = new HeaderDictionary();
            requestFeature.Headers.Add(HeaderNames.Cookie, new StringValues(_sessionCookieKey + "=" + _sessionGuid));
            featureCollection.Set<IHttpRequestFeature>(requestFeature);
            var cookiesFeature = new RequestCookiesFeature(featureCollection);
            httpContext.Request.Cookies = cookiesFeature.Cookies;
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;

            //Set up controller
            var config = InitConfiguration();
            var logger = InitLogger();
            var controller = new DTController(config, logger, db);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            var actionFilter = new DTAuthenticate(config, db);

            //Act
            actionFilter.OnActionExecuting(ctx);

            //Assert
            Assert.IsNotNull(controller.VM, "Controller's VM not set.");
            Assert.IsNull(controller.VM.User, "User should be null.");
        }
    }
}
