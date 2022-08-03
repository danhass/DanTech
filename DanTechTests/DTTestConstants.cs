using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
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

namespace DanTechTests
{
    public class DTTestConstants
    {
        public const string AuthTokensNeedToBeResetKey = "Auth tokens need to be reset";
        public const int DefaultNumberOfTestPropjects = 3;
        public const string LocalHostDomain = "localhost:44324";
        public const string GoogleSigninHandler = "Home/GoogleSignin";
        public const string SessionCookieKey = "dtSessionId";
        public const string TestElementKey = "Test data element";
        public const string TestGoogleCodeTitle = "Google code";
        public const string TestGoogleCodeMistTitle = "Google code - testing";
        public const string TestPlanItemAdditionalTitle = "Additional test plan item title";
        public const string TestPlanItemMinimumTitle = "Minimum test plan item title";       
        public const string TestProjectShortCodePrefix = "TP";
        public const string TestTimeSpanStart = "14:10";
        public const string TestTimeSpanEnd = "16:15";
        public const string TestProjectTitlePrefix = "Test project #";        
        public const string TestSessionId = "01ddb399-850b-4111-a3e6-6afd1c30b605";
        public const string TestStringTrueValue = "1";
        public const string TestUserEmail = "t@t.com"; //Permanent user in the user table that is reserved for testing.
        public const string TestUserFName = "A";
        public const string TestUserLName = "Test";
        public const string TestUserOthername = "tester";
        public const string TestValue = "Test Value";
        public const string TestValue2 = "Test value #2";


        private static dgdb _db = null;
        public static dgdb DB(int numberOfProjects = 0) { if (_db == null) _db = DTDB.getDB(numberOfProjects); return _db; }


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

        public static DTController InitializeDTController(dgdb db, string host, bool withLoggedInUser, IConfiguration config)
        {
            //Set up db
            if (withLoggedInUser)
            {
                var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
                var testSession = (from x in db.dtSessions where x.user == testUser.id select x).FirstOrDefault();
                if (testSession == null)
                {
                    testSession = new dtSession() { user = testUser.id, hostAddress = IPAddress.Loopback.ToString() };
                    db.dtSessions.Add(testSession);
                }
                testSession.expires = DateTime.Now.AddDays(1);
                testSession.session = DTTestConstants.TestSessionId;
                db.SaveChanges();
            } 

            //Set up controller
            var logger = DTTestConstants.InitLogger();
            var controller = new DTController(config, logger, db);

            return controller;
        }

        public static DefaultHttpContext InitializeContext(string host, bool withLoggedInUser)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(host);
            var requestFeature = new HttpRequestFeature();
            var featureCollection = new FeatureCollection();
            requestFeature.Headers = new HeaderDictionary();
            if (withLoggedInUser) requestFeature.Headers.Add(HeaderNames.Cookie, new StringValues(DTTestConstants.SessionCookieKey + "=" + DTTestConstants.TestSessionId));
            featureCollection.Set<IHttpRequestFeature>(requestFeature);
            var cookiesFeature = new RequestCookiesFeature(featureCollection);
            httpContext.Request.Cookies = cookiesFeature.Cookies;
            httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
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
                DTDBDataService.ClearTestData();
            }
        }
    }
}
