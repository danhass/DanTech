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
using System.Diagnostics;

namespace DanTechTests
{
    [TestClass]
    public class GoogleAuthServiceTests
    {
        private const string TEST_USER_EMAIL = "testemail@email.com";
        private const string TEST_USER_FNAME = "Test Given Name";
        private const string TEST_USER_LNAME = "Test Family Name";
        private const string TEST_AUTH_CODE = "TestAccessToken-123456";
        private const string TEST_REFRESH_CODE = "TestRefreshToken-654321";
        private const string TEST_REMOTE_IP = "1.1.1.1";

        public TestContext TestContext { get; set; }

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
            string url = DTGoogleAuthService.AuthService(DTTestConstants.LocalHostDomain, DTTestConstants.GoogleSigninHandler, config);

            //Assert
            Assert.IsTrue(url.StartsWith("https://accounts.google.com/o/oauth2/v2/auth"), "Google auth url does not have proper beginning.");
            Assert.IsTrue(url.Contains("https://localhost:44324/Home/GoogleSignin"), "Google auth url does not have proper return uri.");
        }

        [TestMethod()]
        public void AuthTokenTest_GetAuthToken()
        {
            //Arrange
            var db = DTTestConstants.DB(DTTestConstants.DefaultNumberOfTestPropjects);
            var badTokens = (from x in db.dtMiscs where x.title == DTTestConstants.AuthTokensNeedToBeResetKey select x).FirstOrDefault();
            if (badTokens != null )
            {
                Assert.Inconclusive("AuthTokenTest_GetAuthToken is not run because the auth tokens need to be reset for a valid test.");
                Console.WriteLine("AuthTokenTest_GetAuthToken is not run because the auth tokens need to be reset for a valid test.");
                return;
            }
            var datum = (from x in db.dtTestData where x.title == "Google code" select x).FirstOrDefault();
            
            //Act
            var tokens = DTGoogleAuthService.AuthToken(datum.value, DTTestConstants.LocalHostDomain);
            var badCodeToken = DTGoogleAuthService.AuthToken("1234", DTTestConstants.LocalHostDomain);
            badTokens = new dtMisc() { title = DTTestConstants.AuthTokensNeedToBeResetKey, value = "1" };
            db.dtMiscs.Add(badTokens);
            db.SaveChanges();

            //Assert
            Assert.IsNotNull(datum, "Could not find Google code in test data (dtTextData).");
            Assert.IsFalse(string.IsNullOrEmpty(datum.value), "Google code is null or empty.");
            Assert.IsFalse(string.IsNullOrEmpty(tokens["AccessToken"]), "Google Auth Service did not return an auth token for a (presumed) good Google code.");
            Assert.IsTrue(string.IsNullOrEmpty(badCodeToken["AccessToken"]), "(Presumed) bad Google code should haver return an empty string for auth token.");            
        }

        [TestMethod]
        public void UserInfo_FromAccessToken()
        {
            //Arrange
            var db = DTTestConstants.DB(DTTestConstants.DefaultNumberOfTestPropjects);
            var goodUser = (from x in db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();

            //Act
            goodUser.token = DTGoogleAuthService.RefreshAuthToken(goodUser.refreshToken);
            db.SaveChanges();
            var userInfo = DTGoogleAuthService.GetUserInfo(goodUser.token);

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(goodUser.email), "Could not find known good user.");
            Assert.AreEqual(goodUser.email, userInfo.Email, "Did not retrieve expected info.");
        }

        [TestMethod] 
        public void UserInfo_FromRefreshToken()
        {
            //Arrange
            var db = DTTestConstants.DB(DTTestConstants.DefaultNumberOfTestPropjects);
            var goodUser = (from x in db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();

            //Act
            var userInfo = DTGoogleAuthService.GetUserInfo(TEST_AUTH_CODE, goodUser.refreshToken);           

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(goodUser.email), "Could not find known good user.");
            Assert.AreEqual(goodUser.email, userInfo.Email, "Did not retrieve expected info.");
        }

        [TestMethod]
        public void AuthTokenTest_RefreshAuthToken()
        {
            //Arrange
            var db = DTTestConstants.DB(DTTestConstants.DefaultNumberOfTestPropjects);
            var goodUser = (from x in db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();
            var config = InitConfiguration();

            //Act
            var token = DTGoogleAuthService.RefreshAuthToken(goodUser.refreshToken);

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(token), "Auth token not refreshed.");
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
            string sessionId = DTGoogleAuthService.SetLogin(userinfo, ctx, db, TEST_AUTH_CODE, TEST_REFRESH_CODE);
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
            Assert.AreEqual(session.userNavigation.refreshToken, TEST_REFRESH_CODE, "User info not set correctly");
        }

    }
}
