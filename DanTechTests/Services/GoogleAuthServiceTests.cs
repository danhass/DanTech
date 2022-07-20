using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;
using DanTech.Data;
using Google.Apis.Oauth2.v2.Data;
using DanTechTests.Data;
using DanTech.Services;
using System;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace DanTechTests
{
    [TestClass]
    public class GoogleAuthServiceTests
    {
        private const string localHostDomain = "localhost:44324";
        private const string TEST_USER_EMAIL = "testemail@email.com";
        private const string TEST_USER_FNAME = "Test Given Name";
        private const string TEST_USER_LNAME = "Test Family Name";
        private const string TEST_AUTH_CODE = "TestAccessToken-123456";
        private const string TEST_REMOTE_IP = "1.1.1.1";

        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
              .AddJsonFile("appsettings.json")
               .AddEnvironmentVariables()
               .Build();
            return config;
        }   

        [TestMethod()]
        public void GoogleAuthEndpoint()
        {
            //Arrange
            var config = InitConfiguration();

            //Act
            string url = DTGoogleAuthService.AuthService("localhost:44324", "Home/GoogleSignin", config);

            //Assert
            Assert.IsTrue(url.StartsWith("https://accounts.google.com/o/oauth2/v2/auth"), "Google auth url does not have proper beginning.");
            Assert.IsTrue(url.Contains("https://localhost:44324/Home/GoogleSignin"), "Google auth url does not have proper return uri.");
        }

        [TestMethod()]
        public void AuthTokenTest_GetAuthToken()
        {

            //Arrange
            var db = DTDB.getDB();
            var config = InitConfiguration();
            var datum = (from x in db.dtTestData where x.title == "Google code" select x).FirstOrDefault();
            
            //Act
            string token = DTGoogleAuthService.AuthToken(datum.value, localHostDomain, config);
            string badCodeToken = DTGoogleAuthService.AuthToken("1234", localHostDomain, config);

            //Assert
            Assert.IsNotNull(datum, "Could not find Google code in test data (dtTextData).");
            Assert.IsFalse(string.IsNullOrEmpty(datum.value), "Google code is null or empty.");
            Assert.IsFalse(string.IsNullOrEmpty(token), "Google Auth Service did not return an auth token for a (presumed) good Google code.");
            Assert.IsTrue(string.IsNullOrEmpty(badCodeToken), "(Presumed) bad Google code should haver return an empty string for auth token.");            
        } 

        [TestMethod]
        public void SetLoginTest()
        {

            //Arrange
            Userinfo userinfo = new Userinfo() { Email = TEST_USER_EMAIL, FamilyName = TEST_USER_LNAME, GivenName = TEST_USER_FNAME };
            HttpContext ctx = new DefaultHttpContext();
            ctx.Connection.RemoteIpAddress = IPAddress.Loopback;
            dgdb db = DTDB.getDB();

            //Act
            string sessionId = DTGoogleAuthService.SetLogin(userinfo, ctx, db, TEST_AUTH_CODE);
            Guid sessionAsGuid = Guid.Empty;
            Guid.TryParse(sessionId, out sessionAsGuid);
            var setCookie = ctx.Response.GetTypedHeaders().SetCookie.ToList()[0];
            var ipAddress = ctx.Connection.RemoteIpAddress.ToString();
            var user = (from x in db.dtUsers where x.email == TEST_USER_EMAIL select x).FirstOrDefault();
            var numUsers = (from x in db.dtUsers where x.email == TEST_USER_EMAIL select x).ToList().Count;
            var session = (from x in db.dtSessions where x.session == sessionId select x).FirstOrDefault();
            db.dtSessions.Remove(session);
            db.dtUsers.Remove(user);
            db.SaveChanges();

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(sessionId), "Session id should be a string that can be parsed into a guid.");
            Assert.AreNotEqual(sessionAsGuid, Guid.Empty, "Session should not be an empty GUID.");
            Assert.IsNotNull(setCookie, "Session cookie not set.");
            Assert.IsNotNull(session, "Session not saved to db.");
            Assert.AreEqual(session.hostAddress, ipAddress, "The remote ip address is not set on the session.");
            Assert.AreEqual(session.user, user.id, "Session user not properly set.");

        }

    }
}
