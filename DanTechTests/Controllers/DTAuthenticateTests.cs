using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using DanTech.Controllers;
using Microsoft.Extensions.Configuration;
using DanTechTests.Data;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc.Controllers;
using DanTech.Services;

namespace DanTechTests
{
    [TestClass]
    public class DTAuthenticateTests
    {

        private IDTDBDataService _db = null;
        private IConfiguration _config = null;

        public DTAuthenticateTests()
        {
            if (_config == null) _config = DTTestOrganizer.InitConfiguration();
            if (_db == null)
            {
                var cfg = DTTestOrganizer.InitConfiguration();
                _db = DTTestOrganizer.DataService();
            }
        }
        private IDTDBDataService GetDB()
        {
            if (_config == null) _config = DTTestOrganizer.InitConfiguration();
            if (_db == null)
            {
                var cfg = DTTestOrganizer.InitConfiguration();
                _db = DTTestOrganizer.DataService();
            }
            return _db;
        }
        private DTController ExecuteWithLoggedInUser(string host)
        {
            var controller = DTTestOrganizer.InitializeDTController(_db, true);
            var httpContext = DTTestOrganizer.InitializeContext(host, true);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(_config, _db);

            //Act
            actionFilter.OnActionExecuting(ctx);

            return controller;
        }

        private void RemoveTestSession()
        {
            var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            var testSession = _db.Sessions.Where(x => x.user == testUser.id).FirstOrDefault();
            _db.Delete(testSession);
        }

        [TestMethod]
        public void ActionExecutingTest_SetTestEnv()
        {
            //Arrange

            //Act
            var controller = ExecuteWithLoggedInUser(DTTestConstants.LocalHostDomain);

            //Clean up
            RemoveTestSession();

            //Assert
            Assert.IsTrue(controller.VM.TestEnvironment, "path that begins with localhost should result in test env flag of true.");           
        }

        [TestMethod]
        public void ActionExecutingTest_SetNotTestEnv()
        {
            //Arrange
 
            //Act
            var controller = ExecuteWithLoggedInUser(IPAddress.Loopback.ToString());
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Clean up
            RemoveTestSession();

            //Assert
            Assert.IsFalse(controller.VM.TestEnvironment, "path that does not begin with localhost should result in test env flag of false.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }


        [TestMethod]
        public void ActionExecutingTest_UserLoggedIn()
        {
            //Arrange
            var db = DTDB.getDB();

            //Act
            var controller = ExecuteWithLoggedInUser(DTTestConstants.LocalHostDomain);

            //Clean up db - test user is left in place
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.IsNotNull(controller.VM, "Controller's VM not set.");
            Assert.IsNotNull(controller.VM.User, "User not set on VM.");
            Assert.AreEqual(controller.VM.User.email, DTTestConstants.TestKnownGoodUserEmail, "User set incorrectly");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }

        [TestMethod]
        public void ActionExecutingTest_SessionInPostData()
        {
            //Arrange
            var config = DTTestOrganizer.InitConfiguration();
            var db = _db;
            var controller = DTTestOrganizer.InitializeDTController(db, true);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.LocalHostDomain);    
            httpContext.Request.Headers.Add(HeaderNames.ContentType, new StringValues("application/x-www-form-urlencoded"));
            var form = new Dictionary<string, StringValues>();
            StringValues val = new StringValues(DTTestOrganizer.TestSession.session);
            form["sessionId"] = val;
            httpContext.Request.Form = new FormCollection(form);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            ctx.ActionArguments["sessionId"] = DTTestOrganizer.TestSession.session;
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, db);

            actionFilter.OnActionExecuting(ctx);
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.IsNotNull(controller.VM.User, "Could not set user from post data.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }

        [TestMethod]
        public void ActionExecutingTest_SessionOnQueryString()
        {
            //Arrange 
            var config = DTTestOrganizer.InitConfiguration();
            var db = _db;
            var controller = DTTestOrganizer.InitializeDTController(db, true);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.LocalHostDomain);
            httpContext.Request.QueryString = new QueryString("?sessionId=" + DTTestOrganizer.TestSession.session);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, db);

            //Action
            actionFilter.OnActionExecuting(ctx);
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.IsNotNull(controller.VM.User, "Coult not set user from query string");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }

        [TestMethod]
        public void ActionExectutingTest_SessionQueryStringWithInvalid()
        {
            //Arrange
            var config = DTTestOrganizer.InitConfiguration();
            var db = _db;
            var controller = DTTestOrganizer.InitializeDTController(db, true, "");
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.TestRemoteHost);
            httpContext.Request.QueryString = new QueryString("?sessionId=" + Guid.Empty.ToString());
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, db);

            //Action
            actionFilter.OnActionExecuting(ctx);
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.AreEqual(0,controller.VM.User.id, "Invalid user from query string.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }

        [TestMethod]
        public void ActionExecutingTest_UserNotLoggedIn()
        {
            //Arrange

            //Set up db
            var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault(); ;

            //Remove any session for this user
            var testSession = _db.Sessions.Where(x => x.user == testUser.id).FirstOrDefault();
            
            //Set up context
            var httpContext = new DefaultHttpContext();
            var requestFeature = new HttpRequestFeature();
            var featureCollection = new FeatureCollection();
            requestFeature.Headers = new HeaderDictionary();
            requestFeature.Headers.Add(HeaderNames.Cookie, new StringValues(DTTestOrganizer.TestSession.session + "=" + DTTestOrganizer.TestSession.session));
            requestFeature.Headers.Add(HeaderNames.ContentType, new StringValues("application/x-www-form-urlencoded"));
            featureCollection.Set<IHttpRequestFeature>(requestFeature);
            var cookiesFeature = new RequestCookiesFeature(featureCollection);
            httpContext.Request.Cookies = cookiesFeature.Cookies;
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(DTTestConstants.TestRemoteHostAddress);

            //Set up controller
            var config = DTTestOrganizer.InitConfiguration();
            var logger = DTTestOrganizer.InitLogger();
            var controller = new DTController(config, logger, _db);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, _db);

            //Act
            actionFilter.OnActionExecuting(ctx);
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.IsNotNull(controller.VM, "Controller's VM not set.");
            Assert.AreEqual(0,controller.VM.User.id, "User should be null.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }
    }
}
