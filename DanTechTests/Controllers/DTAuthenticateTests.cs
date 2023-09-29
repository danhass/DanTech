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
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Collections.Specialized;
using DanTech.Services;

namespace DanTechTests
{
    [TestClass]
    public class DTAuthenticateTests
    {

        private IDTDBDataService _db = null;

        private IDTDBDataService GetDB()
        {
            if (_db == null)
            {
                var cfg = DTTestOrganizer.InitConfiguration();
                _db = new DTDBDataService(cfg.GetConnectionString("DG"));
            }
            return _db;
        }
        private DTController ExecuteWithLoggedInUser(string host)
        {
            var config = DTTestOrganizer.InitConfiguration();
            var db = new DTDBDataService(config.GetConnectionString("DG"));
            var controller = DTTestOrganizer.InitializeDTController(db, true);
            var httpContext = DTTestOrganizer.InitializeContext(host, true);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, db.dtdb());

            //Act
            actionFilter.OnActionExecuting(ctx);

            return controller;
        }

        private void RemoveTestSession()
        {
            var db = GetDB();
            var testUser = db.Users().Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            var testSession = db.Sessions().Where(x => x.user == testUser.id).FirstOrDefault();
            db.Delete(testSession);
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
            var db = new DTDBDataService(config.GetConnectionString("DG"));
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
            var actionFilter = new DTAuthenticate(config, db.dtdb());

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
            var db = new DTDBDataService(config.GetConnectionString("DG"));
            var controller = DTTestOrganizer.InitializeDTController(db, true);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.LocalHostDomain);
            httpContext.Request.QueryString = new QueryString("?sessionId=" + DTTestOrganizer.TestSession.session);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, db.dtdb());

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
            var db = new DTDBDataService(config.GetConnectionString("DG"));
            var controller = DTTestOrganizer.InitializeDTController(db, true, "");
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.TestRemoteHost);
            httpContext.Request.QueryString = new QueryString("?sessionId=" + Guid.Empty.ToString());
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, db.dtdb());

            //Action
            actionFilter.OnActionExecuting(ctx);
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.IsNull(controller.VM.User, "Invalid user from query string.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }

        [TestMethod]
        public void ActionExecutingTest_UserNotLoggedIn()
        {
            //Arrange

            //Set up db
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault(); ;

            //Remove any session for this user
            var testSession = (from x in db.dtSessions where x.user == testUser.id select x).FirstOrDefault();
            
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
            var controller = new DTController(config, logger, db);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, db);

            //Act
            actionFilter.OnActionExecuting(ctx);
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.IsNotNull(controller.VM, "Controller's VM not set.");
            Assert.IsNull(controller.VM.User, "User should be null.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }
    }
}
