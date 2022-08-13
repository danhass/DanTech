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
        private DTController ExecuteWithLoggedInUser(dgdb db, string host)
        {
            var config = DTTestConstants.InitConfiguration();
            var controller = DTTestConstants.InitializeDTController(db, true);
            var httpContext = DTTestConstants.InitializeContext(host, true);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            var actionFilter = new DTAuthenticate(config, db);

            //Act
            actionFilter.OnActionExecuting(ctx);

            return controller;
        }

        private void RemoveTestSession(dgdb db)
        {
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var testSession = (from x in db.dtSessions where x.user == testUser.id select x).FirstOrDefault();
            if (testSession != null)
            {
                db.dtSessions.Remove(testSession);
                db.SaveChanges();
            }
        }

        [TestMethod]
        public void ActionExecutingTest_SetTestEnv()
        {
            //Arrange
            var db = DTDB.getDB();

            //Act
            var controller = ExecuteWithLoggedInUser(db, DTTestConstants.LocalHostDomain);

            //Clean up
            RemoveTestSession(db);

            //Assert
            Assert.IsTrue(controller.VM.TestEnvironment, "path that begins with localhost should result in test env flag of true.");           
        }

        [TestMethod]
        public void ActionExecutingTest_SetNotTestEnv()
        {
            //Arrange
            var db = DTDB.getDB();

            //Act
            var controller = ExecuteWithLoggedInUser(db, IPAddress.Loopback.ToString());

            //Clean up
            RemoveTestSession(db);

            //Assert
            Assert.IsFalse(controller.VM.TestEnvironment, "path that does not begin with localhost should result in test env flag of false.");
        }


        [TestMethod]
        public void ActionExecutingTest_UserLoggedIn()
        {
            //Arrange
            var db = DTDB.getDB();

            //Act
            var controller = ExecuteWithLoggedInUser(db, DTTestConstants.TestRemoteHost);

            //Clean up db - test user is left in place
            RemoveTestSession(db);

            //Assert
            Assert.IsNotNull(controller.VM, "Controller's VM not set.");
            Assert.IsNotNull(controller.VM.User, "User not set on VM.");
            Assert.AreEqual(controller.VM.User.email, DTTestConstants.TestUserEmail, "User set incorrectly");
        }

        [TestMethod]
        public void ActionExecutingTest_UserNotLoggedIn()
        {
            //Arrange

            //Set up db
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
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
            requestFeature.Headers.Add(HeaderNames.Cookie, new StringValues(DTTestConstants.TestSessionId + "=" + DTTestConstants.TestSessionId));
            featureCollection.Set<IHttpRequestFeature>(requestFeature);
            var cookiesFeature = new RequestCookiesFeature(featureCollection);
            httpContext.Request.Cookies = cookiesFeature.Cookies;
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(DTTestConstants.TestRemoteHostAddress);

            //Set up controller
            var config = DTTestConstants.InitConfiguration();
            var logger = DTTestConstants.InitLogger();
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
