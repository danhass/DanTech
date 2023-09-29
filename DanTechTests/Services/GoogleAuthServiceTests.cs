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
        private dtdb _db = null;
        private dtUser _goodUser = null;

        public TestContext TestContext { get; set; }

        public GoogleAuthServiceTests()
        {
            _db = DTDB.getDB();
            _goodUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();

        }

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
            var google = new DTGoogleAuthService();
            string url = google.AuthService(DTTestConstants.LocalHostDomain, DTTestConstants.GoogleSigninHandler, config);

            //Assert
            Assert.IsTrue(url.StartsWith("https://accounts.google.com/o/oauth2/v2/auth"), "Google auth url does not have proper beginning.");
            Assert.IsTrue(url.Contains("https://localhost:44324/Home/GoogleSignin"), "Google auth url does not have proper return uri.");
        }

        [TestMethod()]
        public void AuthTokenTest_GetAuthToken()
        {
            //Arrange
            var google = DTTestOrganizer.Google();
            var datum = DTTestConstants.TestGoogleCode;
            
            //Act
            var tokens = google.AuthToken(datum, DTTestConstants.LocalHostDomain);
            var badCodeToken = google.AuthToken("1234", DTTestConstants.LocalHostDomain);

            //Assert
            Assert.IsNotNull(datum, "Could not find Google code in test data (dtTextData).");
            Assert.IsFalse(string.IsNullOrEmpty(datum), "Google code is null or empty.");
            Assert.IsFalse(string.IsNullOrEmpty(tokens["AccessToken"]), "Google Auth Service did not return an auth token for a (presumed) good Google code.");
            Assert.IsTrue(string.IsNullOrEmpty(badCodeToken["AccessToken"]), "(Presumed) bad Google code should haver return an empty string for auth token.");            
        }        

        [TestMethod]
        public void UserInfo_FromAccessToken()
        {

            //Arrange

            //Act
            var google = new DTGoogleAuthService();
            var userInfo = google.GetUserInfo(_goodUser.token);

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(_goodUser.email), "Could not find known good user.");
            Assert.IsNotNull(userInfo, "Could not get userInfo; May need to relog with " + DTTestConstants.TestKnownGoodUserEmail);
            Assert.AreEqual(_goodUser.email, userInfo.Email, "Did not retrieve expected info. May need to relog with " + DTTestConstants.TestKnownGoodUserEmail);
        }

        [TestMethod] 
        public void UserInfo_FromRefreshToken()
        {
            //Arrange
            if (string.IsNullOrEmpty(_goodUser.refreshToken)) Assert.Inconclusive("Need to relog with " + DTTestConstants.TestKnownGoodUserEmail);

            //Act
            var userInfo = new DTGoogleAuthService().GetUserInfo("", _goodUser.refreshToken);           

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(_goodUser.email), "Could not find known good user.");
            Assert.AreEqual(_goodUser.email, userInfo.Email, "Did not retrieve expected info.");
        }

        [TestMethod]
        public void AuthTokenTest_RefreshAuthToken()
        {
            //Arrange
            if (string.IsNullOrEmpty(_goodUser.refreshToken)) Assert.Inconclusive();
            var config = InitConfiguration();

            //Act
            var token = new DTGoogleAuthService().RefreshAuthToken(_goodUser.refreshToken);

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(token), "Auth token not refreshed.");
        }

        [TestMethod]
        public void SetLoginTest()
        {
            //Arrange
            Userinfo userinfo = new Userinfo() { Email = TEST_USER_EMAIL, FamilyName = TEST_USER_LNAME, GivenName = TEST_USER_FNAME };
            HttpContext ctx = new DefaultHttpContext();
            ctx.Connection.RemoteIpAddress = IPAddress.Parse(DTTestConstants.TestRemoteHostAddress);
            ctx.Request.Host = new HostString(DTTestConstants.TestRemoteHost);
            var cfg = InitConfiguration();
            DTDBDataService db = new DTDBDataService(cfg.GetConnectionString("DG"));

            //Act
            var login = new DTGoogleAuthService().SetLogin(userinfo, ctx, db, TEST_AUTH_CODE, TEST_REFRESH_CODE);
            Guid sessionAsGuid = Guid.Empty;
            Guid.TryParse(login.Session, out sessionAsGuid);
            var setCookie = ctx.Response.GetTypedHeaders().SetCookie.ToList()[0];
            var requestHost = ctx.Request.Host.Value;
            var user = db.Users().Where(x => x.email == TEST_USER_EMAIL).FirstOrDefault();
            var session = db.Sessions().Where(x => x.session == login.Session).FirstOrDefault();
            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(login.Session), "Session id should be a string that can be parsed into a guid.");
            Assert.AreNotEqual(sessionAsGuid, Guid.Empty, "Session should not be an empty GUID.");
            Assert.IsNotNull(setCookie, "Session cookie not set.");
            Assert.IsNotNull(session, "Session not saved to db.");
            Assert.AreEqual(session.hostAddress, requestHost, "The remote host is not set on the session.");
            Assert.AreEqual(session.user, user.id, "Session user not properly set.");
            Assert.AreEqual(session.userNavigation.refreshToken, TEST_REFRESH_CODE, "User info not set correctly");

            //Antiseptic
            db.Delete(session);
            db.Delete(user);
        }

        [TestMethod]
        public void SetLogin_SessionId()
        {
            //Arrange
            var cfg = InitConfiguration();
            DTDBDataService db = new DTDBDataService(cfg.GetConnectionString("DG"));
            var ctrl = DTTestOrganizer.InitializeDTController(db, true);
            var testUser = db.Users().Where(x => x.email == DTTestConstants.TestKnownGoodUserEmail).FirstOrDefault();
            var testSession = db.Sessions().Where(x => x.user == testUser.id).FirstOrDefault();
            var log = "";

            //Act
            var google = new DTGoogleAuthService();
            var badLogin = google.SetLogin(new Guid().ToString(), DTTestConstants.TestRemoteHost, db, ref log);
            var goodLogin = google.SetLogin(testSession.session, DTTestConstants.LocalHostDomain, db, ref log);

            //Assert
            Assert.IsNotNull(testUser, "Could not establish test user.");
            Assert.IsNotNull(testSession, "Could not establish test session.");
            Assert.IsTrue(string.IsNullOrEmpty(badLogin.Session), "Bad session id still gave a login");
            Assert.AreEqual(testSession.session, goodLogin.Session, "Login session incorrect.");
            Assert.AreEqual(testUser.email, goodLogin.Email, "Login email incorrect.");
            Assert.AreEqual(testUser.fName, goodLogin.FName, "Login first name incorrect.");
            Assert.AreEqual(testUser.lName, goodLogin.LName, "Login last name incorrect");


        }

    }
}
