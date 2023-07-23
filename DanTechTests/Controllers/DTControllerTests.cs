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
using System.Reflection;
using System.Linq;
using DanTech.Controllers;
using Microsoft.Extensions.Configuration;
using DanTechTests.Data;
using DanTech.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using DanTech.Models.Data;

namespace DanTechTests.Controllers
{
    [TestClass]
    public class DTControllerTests
    {
        private dtdb _db = null;
        private dtUser _goodUser = null;
        private dtSession _goodSession = null;
        private dtUser _knownGoodUser = null;
        public DTControllerTests()
        {
            _db = DTDB.getDB();
            _goodUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            _goodSession = (from x in _db.dtSessions where x.user == _goodUser.id select x).FirstOrDefault();
            _knownGoodUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();
        }

        [TestMethod]
        public void DTControllerTest_loggedIn()
        {
            //Arrange
            var controller = DTTestOrganizer.InitializeDTController(_db, true);           

            //Assert
            Assert.IsNotNull(controller, "Did not successfully instantiate controller.");
        }

        [TestMethod]
        public void HomeController_GoodLogin()
        {
            //Arrange
            var controller = DTTestOrganizer.InitializeHomeController(_db , _goodSession != null, DTTestConstants.TestUserEmail, _goodSession == null ? "" : _goodSession.session);
            _goodSession = (from x in _db.dtSessions where x.user == _goodUser.id select x).FirstOrDefault();

            //Act
            var login = controller.EstablishSession();
            dtLogin res = (dtLogin)login.Value;
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.IsNotNull(login, "Login is null.");
            Assert.AreEqual(_goodUser.email, res.Email, "Login email is incorrect.");
            Assert.AreEqual(_goodSession.session, res.Session, "Session guid is incorrect.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }
                
        [TestMethod]
        public void HomeController_LoginFromMultipleLocations()
        {
            //Arrange
            dtSession firstSession = new dtSession() { user = _goodUser.id, session = Guid.NewGuid().ToString(), hostAddress = DTTestConstants.TestRemoteHost, expires = DateTime.Now.AddDays(30) };
            dtSession secondSession = new dtSession() { user = _goodUser.id, session = Guid.NewGuid().ToString(), hostAddress = "AnoterTest.com", expires = DateTime.Now.AddDays(30) };
            
            _db.dtSessions.Add(firstSession);
            _db.dtSessions.Add(secondSession);
            _db.SaveChanges();

            _db.dtSessions.Remove(firstSession);
            _db.dtSessions.Remove(secondSession);
            _db.SaveChanges();

            //Assert
            Assert.IsNotNull(firstSession);
            Assert.IsNotNull(secondSession);
        }
        

        [TestMethod]
        public void HomeController_LoginWithSessionId_NoCookie()
        {
            //Arrange
            var controller = DTTestOrganizer.InitializeHomeController(_db, _goodSession == null, "", _goodSession == null ? "" : _goodSession.session);
            _goodSession = (from x in _db.dtSessions where x.user == _goodUser.id select x).FirstOrDefault();
            Console.WriteLine("Good session: " + _goodSession.id);

            //Act
            var login = controller.Login(_goodSession.session);
            dtLogin res = (dtLogin)login.Value;

            //Assert
            Assert.IsNotNull(login, "Login is null.");
            Assert.AreEqual(_goodUser.email, res.Email, "Login email is incorrect.");
            Assert.AreEqual(_goodSession.session, res.Session, "Session guid is incorrect.");
        }

        [TestMethod]
        public void HomeController_LoginWithSessionId_DifferentHostAddress()
        {
            //Arrange
            var controller = DTTestOrganizer.InitializeHomeController(_db, true);
            var testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();
            var testSession = (from x in _db.dtSessions where x.user == testUser.id select x).FirstOrDefault();
            string hostAddressNeedsToBeRestored = testSession.hostAddress;
            testSession.hostAddress = "0:0:0:0";

            //Act
            var login = controller.Login(testSession.session);
            dtLogin res = (dtLogin)login.Value;
            testSession.hostAddress = hostAddressNeedsToBeRestored;

            //Assert
            Assert.IsNotNull(login, "Bad session+host login should still return a non-null login object.");
            Assert.IsTrue(string.IsNullOrEmpty(res.Email), "Bad session+host login should have null email. Instead has " + res.Email);
            Assert.IsTrue(string.IsNullOrEmpty(res.Session), "Bad session+host login should have null session.");
            Assert.IsFalse(string.IsNullOrEmpty(res.Message), "Bad session+host login should return a message.");
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
            var session = (from x in _db.dtSessions where x.session == login.Session select x).FirstOrDefault();
            var cookie = "";
            foreach (var header in controller.Response.Headers.Values)
            {
                if (header.Count > 0)
                {
                    foreach (var h in header)
                    {
                        if (h.StartsWith(DTConstants.SessionKey))
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
            var controller = DTTestOrganizer.InitializeHomeController(_db, false);
            controller.SetGoogle(DTTestOrganizer.Google());

            //Act
            var sessionJson = controller.EstablishSession(DTTestConstants.TestGoogleCode, true, DTTestConstants.LocalHostDomain);
            var login = (dtLogin)(sessionJson.Value);
            string expectSession = login.Session;
            var storedSession = controller.VM == null || controller.VM.User == null ? "None" : (from x in _db.dtSessions where x.user == controller.VM.User.id select x.session).FirstOrDefault();
            
            //Assert
            Assert.IsNotNull(sessionJson, "Did not receive json result.");
            Assert.IsNotNull(storedSession, "Did not store json or return user information.");
            Assert.AreEqual(storedSession, expectSession, "What is stored is not what is expected.");
       
        }

    }
}
