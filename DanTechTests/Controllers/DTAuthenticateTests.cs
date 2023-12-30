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
using System.Threading.Tasks;
using DanTech.Data;
using Org.BouncyCastle.Utilities.Net;

namespace DanTechTests
{
    [TestClass]
    public class DTAuthenticateTests
    {

        private IConfiguration _config = null;

        public DTAuthenticateTests()
        {
            if (_config == null) _config = DTTestOrganizer.InitConfiguration();
        }
        private DTController ExecuteWithLoggedInUser(string host)
        {
            dtdb dbctx = new dtdb(_config.GetConnectionString("DG"));
            var db = new DTDBDataService(_config, dbctx);
            var usr = db.Users.Where(x => x.email == DTTestConstants.TestKnownGoodUserEmail).FirstOrDefault();
            var session = db.Sessions.Where(x => x.user == usr.id && x.hostAddress == DTTestConstants.LocatHostIP).FirstOrDefault();
            var controller = DTTestOrganizer.InitializeDTController(true);
            var httpContext = DTTestOrganizer.InitializeContext(host, true, session.session);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(_config, db);

            //Act
            actionFilter.OnActionExecuting(ctx);

            return controller;
        }

        private void RemoveTestSession()
        {
            /*
            var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            var testSession = _db.Sessions.Where(x => x.user == testUser.id).FirstOrDefault();
            _db.Delete(testSession);
            */
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
            var controller = ExecuteWithLoggedInUser(System.Net.IPAddress.Loopback.ToString());
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"][0];

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

            //Act
            var controller = ExecuteWithLoggedInUser(DTTestConstants.LocatHostIP);

            //Clean up db - test user is left in place
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"][0];

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
            var config = _config;
            var dbctx = new dtdb(_config.GetConnectionString("DG"));
            var db = new DTDBDataService(_config, dbctx);
            var controller = DTTestOrganizer.InitializeDTController(true);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.LocalHostDomain);    
            httpContext.Request.Headers.Add(HeaderNames.ContentType, new StringValues("application/x-www-form-urlencoded"));
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(DTTestConstants.LocatHostIP);
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
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"][0];

            //Assert
            Assert.IsNotNull(controller.VM.User, "Could not set user from post data.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
            
        }

        [TestMethod]
        public void ActionExecutingTest_SessionOnQueryString()
        {

            //Arrange 
            var config = _config;
            var dbctx = new dtdb(_config.GetConnectionString("DG"));
            var db = new DTDBDataService(_config, dbctx);
            var controller = DTTestOrganizer.InitializeDTController(true);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.LocalHostDomain);
            httpContext.Request.QueryString = new QueryString("?sessionId=" + DTTestOrganizer.TestSession.session);
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(DTTestConstants.LocatHostIP);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, db);

            //Action
            actionFilter.OnActionExecuting(ctx);
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"][0];

            //Assert
            Assert.IsNotNull(controller.VM.User, "Coult not set user from query string");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
            
        }

        [TestMethod]
        public void ActionExectutingTest_SessionQueryStringWithInvalid()
        {

            //Arrange
            var config = _config;
            var dbctx = new dtdb(_config.GetConnectionString("DG"));
            var db = new DTDBDataService(_config, dbctx);
            var controller = DTTestOrganizer.InitializeDTController(true, null);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.TestRemoteHost);
            httpContext.Request.QueryString = new QueryString("?sessionId=" + Guid.Empty.ToString());
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(DTTestConstants.LocatHostIP);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, db);

            //Action
            actionFilter.OnActionExecuting(ctx);
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"][0];

            //Assert
            Assert.AreEqual(0,controller.VM.User.id, "Invalid user from query string.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
            
        }

        [TestMethod]
        public void ActionExecutingTest_UserNotLoggedIn()
        {
            
            //Arrange            
            var testUser = DTTestOrganizer.TestUser;
            var config = _config;
            var dbctx = new dtdb(_config.GetConnectionString("DG"));
            var db = new DTDBDataService(_config, dbctx);

            //Remove any session for this user
            var testSession = DTTestOrganizer.TestUserSession;
            
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
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(DTTestConstants.TestRemoteHostAddress);

            //Set up controller
            var logger = DTTestOrganizer.InitLogger();
            var controller = new DTController(config, logger, db, dbctx);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, db);

            //Act
            actionFilter.OnActionExecuting(ctx);
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"][0];

            //Assert
            Assert.IsNotNull(controller.VM, "Controller's VM not set.");
            Assert.AreEqual(0,controller.VM.User.id, "User should be null.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
            
        }
    }
}
