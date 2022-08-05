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
        public void HomeController_EstablishSession()
        {
            //Arrange
            var db = DTDB.getDB();
            var controller = DTTestConstants.InitializeHomeController(db, false);
            var goodUser = (from x in db.dtUsers where x.email == DTTestConstants.TestKnownGoodUserEmail select x).FirstOrDefault();
            var cookieCount = 0;

            //Act
            var sessionId = controller.EstablishSession(goodUser.token, goodUser.refreshToken);
            var session = (from x in db.dtSessions where x.session == sessionId select x).FirstOrDefault();
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
            Assert.IsFalse(string.IsNullOrEmpty(sessionId), "EstablishSession did not return a session.");
            Assert.AreEqual(sessionId, cookie, "Session not properly set on cookie.");
            Assert.AreEqual(cookieCount, 1, "More than one dtSessionId cookie.");
        }
 
    }
}
