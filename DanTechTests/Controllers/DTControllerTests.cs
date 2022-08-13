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
            var controller = DTTestConstants.InitializeDTController(db, true);           

            //Assert
            Assert.IsNotNull(controller, "Did not successfully instantiate controller.");
        }

        [TestMethod]
        public void HomeController_GoodLogin()
        {
            //Arrange
            var db = DTDB.getDB();
            var goodUser = (from x in db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();
            var goodSession = (from x in db.dtSessions where x.user == goodUser.id select x).FirstOrDefault();
            var controller = DTTestConstants.InitializeHomeController(db, goodSession == null, "", goodSession == null ? "" : goodSession.session);
            goodSession = (from x in db.dtSessions where x.user == goodUser.id select x).FirstOrDefault();

            //Act
            var login = controller.EstablishSession();
            dtLogin res = (dtLogin)login.Value;
  
            //Assert
            Assert.IsNotNull(login, "Login is null.");
            Assert.AreEqual(goodUser.email, res.Email, "Login email is incorrect.");
            Assert.AreEqual(goodSession.session, res.Session, "Session guid is incorrect.");
        }

        [TestMethod]
        public void HomeController_LoginWithSessionId_NoCookie()
        {
            //Arrange
            var db = DTDB.getDB();
            var goodUser = (from x in db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();
            var goodSession = (from x in db.dtSessions where x.user == goodUser.id select x).FirstOrDefault();
            var controller = DTTestConstants.InitializeHomeController(db, goodSession == null, "", goodSession == null ? "" : goodSession.session);
            goodSession = (from x in db.dtSessions where x.user == goodUser.id select x).FirstOrDefault();

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
            var db = DTDB.getDB();
            var controller = DTTestConstants.InitializeHomeController(db, true);
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var testSession = (from x in db.dtSessions where x.user == testUser.id select x).FirstOrDefault();
            string hostAddressNeedsToBeRestored = testSession.hostAddress;
            testSession.hostAddress = "0:0:0:0";
            db.SaveChanges();

            //Act
            var login = controller.Login(testSession.session);
            dtLogin res = (dtLogin)login.Value;
            testSession.hostAddress = hostAddressNeedsToBeRestored;
            db.SaveChanges();

            //Assert
            Assert.IsNotNull(login, "Bad session+host login should still return a non-null login object.");
            Assert.IsTrue(string.IsNullOrEmpty(res.Email), "Bad session+host login should have null email.");
            Assert.IsTrue(string.IsNullOrEmpty(res.Session), "Bad session+host login should have null session.");
            Assert.IsFalse(string.IsNullOrEmpty(res.Message), "Bad session+host login should return a message.");
        }

        [TestMethod]
        public void HomeController_EstablishSession()
        {
            //Arrange
            var db = DTDB.getDB();
            var controller = DTTestConstants.InitializeHomeController(db, false);
            var goodUser = (from x in db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();
            var cookieCount = 0;

            //Act
            var login = controller.EstablishSession(goodUser.token, goodUser.refreshToken);
            var session = (from x in db.dtSessions where x.session == login.Session select x).FirstOrDefault();
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
            var db = DTDB.getDB();
            var badTokens = (from x in db.dtMiscs where x.title == DTTestConstants.AuthTokensNeedToBeResetKey select x).FirstOrDefault();
            if (!DTTestConstants.TestControl_EstablishSession_with_code) Assert.Inconclusive("AuthTokenTest_GetAuthToken is not run because the auth tokens need to be reset for a valid test.");
            var datum = (from x in db.dtTestData where x.title == "Google code" select x).FirstOrDefault();
            var controller = DTTestConstants.InitializeHomeController(db, false);


            //Act
            var sessionJson = controller.EstablishSession(datum.value, true, DTTestConstants.LocalProtocol + "://" + DTTestConstants.LocalHostDomain);
            string json = sessionJson.Value.ToString();
            string expectSession = json.Split("=")[1].Replace("}", "").Trim();
            var storedSession = controller.VM == null || controller.VM.User == null ? "None" : (from x in db.dtSessions where x.user == controller.VM.User.id select x.session).FirstOrDefault();

            //Assert
            Assert.IsNotNull(sessionJson, "Did not receive json result.");
            Assert.IsNotNull(storedSession, "Did not store json or return user information.");
            Assert.AreEqual(storedSession, expectSession, "What is stored is not what is expected.");
       
        }

    }
}
