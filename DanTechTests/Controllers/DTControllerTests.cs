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
        [TestMethod]
        public void DTControllerTest_loggedIn()
        {
            //Arrange
            var db = DTDB.getDB();            
            var controller = DTTestOrganizer.InitializeDTController(db, true);           

            //Assert
            Assert.IsNotNull(controller, "Did not successfully instantiate controller.");
        }

        [TestMethod]
        public void HomeController_GoodLogin()
        {
            //Arrange
            var db = DTDB.getDB();
            var dal = DTTestOrganizer.DAL_LIVE();
            var goodUser = dal.user(new DGDAL_Email() { Email = DTTestConstants.TestUserEmail });
            var goodSession = dal.session(goodUser.id);
            var controller = DTTestOrganizer.InitializeHomeController(dal , goodSession != null, DTTestConstants.TestUserEmail, goodSession == null ? "" : goodSession.session);
            goodSession = dal.session(goodUser.id);

            //Act
            var login = controller.EstablishSession();
            dtLogin res = (dtLogin)login.Value;
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.IsNotNull(login, "Login is null.");
            Assert.AreEqual(goodUser.email, res.Email, "Login email is incorrect.");
            Assert.AreEqual(goodSession.session, res.Session, "Session guid is incorrect.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");

        }
                
        [TestMethod]
        public void HomeController_LoginFromMultipleLocations()
        {
            //Arrange
            var db = DTDB.getDB();
            var goodUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            dtSession firstSession = new dtSession() { user = goodUser.id, session = Guid.NewGuid().ToString(), hostAddress = DTTestConstants.TestRemoteHost, expires = DateTime.Now.AddDays(30) };
            dtSession secondSession = new dtSession() { user = goodUser.id, session = Guid.NewGuid().ToString(), hostAddress = "AnoterTest.com", expires = DateTime.Now.AddDays(30) };
            db.dtSessions.Add(firstSession);
            db.dtSessions.Add(secondSession);
            db.SaveChanges();

            db.dtSessions.Remove(firstSession);
            db.dtSessions.Remove(secondSession);
            db.SaveChanges();

            //Assert
            Assert.IsNotNull(firstSession);
            Assert.IsNotNull(secondSession);
        }
        

        [TestMethod]
        public void HomeController_LoginWithSessionId_NoCookie()
        {
            //Arrange
            var dal = DTTestOrganizer.DAL_LIVE();
            var goodUser = dal.user(new DGDAL_Email() { Email = DTTestConstants.TestUserEmail });
            var goodSession = dal.session(goodUser.id);
            var controller = DTTestOrganizer.InitializeHomeController(dal, goodSession == null, "", goodSession == null ? "" : goodSession.session);
            goodSession = dal.session(goodUser.id);
            Console.WriteLine("Good session: " + goodSession.id);

            //Act
            var login = controller.Login(goodSession.session);
            dtLogin res = (dtLogin)login.Value;

            //Assert
            Assert.IsNotNull(login, "Login is null.");
            Assert.AreEqual(goodUser.email, res.Email, "Login email is incorrect.");
            Assert.AreEqual(goodSession.session, res.Session, "Session guid is incorrect.");
        }

        [TestMethod]
        public void HomeController_LoginWithSessionId_DifferentHostAddress()
        {
            //Arrange
            var dal = DTTestOrganizer.DAL_LIVE();
            var controller = DTTestOrganizer.InitializeHomeController(dal, true);
            var testUser = dal.user(new DGDAL_Email() { Email = DTTestConstants.TestKnownGoodUserEmail });
            var testSession = dal.session(testUser.id);
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
            var dal = DTTestOrganizer.DAL_LIVE();
            var controller = DTTestOrganizer.InitializeHomeController(dal, false);
            var goodUser = dal.user(new DGDAL_Email() { Email = DTTestConstants.TestKnownGoodUserEmail });
            if (string.IsNullOrEmpty(goodUser.refreshToken)) Assert.Inconclusive();
            var cookieCount = 0;

            //Act
            var login = controller.EstablishSession(goodUser.token, goodUser.refreshToken);
            var session = dal.session(login);            
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
            var dal = DTTestOrganizer.DAL_LIVE();
            //if (!DTTestConstants.TestControl_EstablishSession_with_code) Assert.Inconclusive("AuthTokenTest_GetAuthToken is not run because the auth tokens need to be reset for a valid test.");
            //var datum = (from x in db.dtTestData where x.title == "Google code" select x).FirstOrDefault();
            var controller = DTTestOrganizer.InitializeHomeController(dal, false);
            controller.SetGoogle(DTTestOrganizer.Google());

            //Act
            var sessionJson = controller.EstablishSession(DTTestConstants.TestGoogleCodeTitle, true, DTTestConstants.LocalHostDomain);
            var login = (dtLogin)(sessionJson.Value);
            string expectSession = login.Session;
            var storedSession = controller.VM == null || controller.VM.User == null ? "None" : dal.session(controller.VM.User.id).session;

            //Assert
            Assert.IsNotNull(sessionJson, "Did not receive json result.");
            Assert.IsNotNull(storedSession, "Did not store json or return user information.");
            Assert.AreEqual(storedSession, expectSession, "What is stored is not what is expected.");
       
        }

    }
}
