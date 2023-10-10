using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using DanTech.Data;
using DanTech.Data.Models;
using DanTech.Services;

namespace DanTechTests.Controllers
{
    [TestClass]
    public class DTControllerTests
    {
        private DTDBDataService _db = null;
        private IConfiguration _cfg = null;
        private dtUser _goodUser = null;
        private dtSession _goodSession = null;
        private dtUser _knownGoodUser = null;
        private dtSession _knownGoodSession = null;
        public DTControllerTests()
        {
            _cfg = DTTestOrganizer.InitConfiguration();
            _db = new DTDBDataService(_cfg.GetConnectionString("DG"));
            _goodUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            _goodSession = _db.Sessions.Where(x => x.user == _goodUser.id).FirstOrDefault();
            _knownGoodUser = _db.Users.Where(x => x.email == DTTestConstants.TestKnownGoodUserEmail).FirstOrDefault();
            _knownGoodSession = _db.Sessions.Where(x => x.user == _knownGoodUser.id).FirstOrDefault();
        }

        [TestMethod]
        public void DTControllerTest_loggedIn()
        {
            //Arrange
            var config = DTTestOrganizer.InitConfiguration();
            var db = new DTDBDataService(config.GetConnectionString("DG"));
            var controller = DTTestOrganizer.InitializeDTController(db, true);           

            //Assert
            Assert.IsNotNull(controller, "Did not successfully instantiate controller.");
        }

        [TestMethod]
        public void HomeController_GoodLogin()
        {
            //Arrange
            DTTestOrganizer.SetGoodUserData();
            var controller = DTTestOrganizer.InitializeHomeController(_db , _knownGoodSession != null, DTTestConstants.TestKnownGoodUserEmail, _knownGoodSession == null ? "" : _knownGoodSession.session);
            _knownGoodSession = _db.Sessions.Where(x => x.user == _knownGoodUser.id).FirstOrDefault();

            //Act
            var login = controller.EstablishSession();
            dtLogin res = (dtLogin)login.Value;
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.IsNotNull(login, "Login is null.");
            Assert.AreEqual(_knownGoodUser.email, res.Email, "Login email is incorrect.");
            Assert.AreEqual(_knownGoodSession.session, res.Session, "Session guid is incorrect.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }
                
        [TestMethod]
        public void HomeController_LoginFromMultipleLocations()
        {
            //Arrange
            dtSession firstSession = new dtSession() { user = _goodUser.id, session = Guid.NewGuid().ToString(), hostAddress = DTTestConstants.TestRemoteHost, expires = DateTime.Now.AddDays(30) };
            dtSession secondSession = new dtSession() { user = _goodUser.id, session = Guid.NewGuid().ToString(), hostAddress = "AnoterTest.com", expires = DateTime.Now.AddDays(30) };
            firstSession = _db.Set(firstSession);
            secondSession = _db.Set(secondSession);

            _db.Delete(firstSession);
            _db.Delete(secondSession);

            //Assert
            Assert.IsNotNull(firstSession);
            Assert.IsNotNull(secondSession);
        }
        

        [TestMethod]
        public void HomeController_LoginWithSessionId_NoCookie()
        {
            //Arrange
            var controller = DTTestOrganizer.InitializeHomeController(_db, _knownGoodSession == null, "", _knownGoodSession == null ? "" : _knownGoodSession.session);
            _knownGoodSession = _db.Sessions.Where(x => x.user == _knownGoodUser.id).FirstOrDefault();
            Console.WriteLine("Good session: " + _knownGoodSession.id);

            //Act
            var login = controller.Login(_knownGoodSession.session);
            dtLogin res = (dtLogin)login.Value;

            //Assert
            Assert.IsNotNull(login, "Login is null.");
            Assert.AreEqual(_knownGoodUser.email, res.Email, "Login email is incorrect.");
            Assert.AreEqual(_knownGoodSession.session, res.Session, "Session guid is incorrect.");
        }

        [TestMethod]
        public void HomeController_EstablishSession()
        {
            //Arrange
            var controller = DTTestOrganizer.InitializeHomeController(_db, false);
            if (string.IsNullOrEmpty(_knownGoodUser.refreshToken)) Assert.Inconclusive();
            var cookieCount = 0;

            //Act
            var login = controller.EstablishSession(_knownGoodUser.token, _knownGoodUser.refreshToken);
            var session = _db.Sessions.Where(x => x.session == login.Session);
            var cookie = "";
            foreach (var header in controller.Response.Headers.Values)
            {
                if (header.Count > 0)
                {
                    foreach (var h in header)
                    {
                        if (h.StartsWith("dtSession"))
                        {
                            if (h.Split(";").Length > 0 && h.Split(";")[0].Split("=", 2).Length > 1) cookie = h.Split(";")[0].Split("=", 2)[1];
                            cookieCount = cookieCount + 1;
                        }
                    }
                }
            }

            //Assert
            Assert.IsNotNull(controller, "Home controller could not be initialized.");
            Assert.IsFalse(string.IsNullOrEmpty(login.Session), "EstablishSession did not return a session.");
            Assert.AreEqual(login.Session, cookie, "Session not properly set on cookie.");
            Assert.AreEqual(cookieCount, 1, "More than one dtSessionId cookie.");
        }

        [TestMethod]
        public void HomeController_EstablishSession_GoogleCode()
        {
            //Arrange
            //if (!DTTestConstants.TestControl_EstablishSession_with_code) Assert.Inconclusive("AuthTokenTest_GetAuthToken is not run because the auth tokens need to be reset for a valid test.");
            //var datum = (from x in db.dtTestData where x.title == "Google code" select x).FirstOrDefault();
            DTTestOrganizer.SetGoodUserData();
            var controller = DTTestOrganizer.InitializeHomeController(_db, false);
            controller.SetGoogle(DTTestOrganizer.Google());

            //Act
            var sessionJson = controller.EstablishSession(DTTestConstants.TestGoogleCode, true, DTTestConstants.LocalHostDomain);
            var login = (dtLogin)(sessionJson.Value);
            string expectSession = login.Session;
            var storedSessionObject = controller.VM == null || controller.VM.User == null ? null : _db.Sessions.Where(x => (x.user == controller.VM.User.id && x.hostAddress == DTTestConstants.LocalHostDomain)).FirstOrDefault();
            var storedSession = storedSessionObject == null ? "None" : storedSessionObject.session;

            //Assert
            Assert.IsNotNull(sessionJson, "Did not receive json result.");
            Assert.IsNotNull(storedSession, "Did not store json or return user information.");
            Assert.AreEqual(storedSession, expectSession, "What is stored is not what is expected.");
       
        }

    }
}
