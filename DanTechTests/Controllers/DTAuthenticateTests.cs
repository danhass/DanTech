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

namespace DanTechTests
{
    [TestClass]
    public class DTAuthenticateTests
    {        
        private DTController ExecuteWithLoggedInUser(IDTDPDAL dal, string host)
        {
            var config = DTTestOrganizer.InitConfiguration();
            var controller = DTTestOrganizer.InitializeDTController(dal.GetDB(), true);
            var httpContext = DTTestOrganizer.InitializeContext(host, true);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, dal);

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
            var controller = ExecuteWithLoggedInUser(DTTestOrganizer.DAL_LIVE(), DTTestConstants.LocalHostDomain);

            //Clean up
            RemoveTestSession(db);

            //Assert
            Assert.IsTrue(controller.VM.TestEnvironment, "path that begins with localhost should result in test env flag of true.");           
        }

        [TestMethod]
        public void ActionExecutingTest_SetNotTestEnv()
        {
            //Arrange
            var dal = DTTestOrganizer.DAL_LIVE();

            //Act
            var controller = ExecuteWithLoggedInUser(dal, IPAddress.Loopback.ToString());
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Clean up
            RemoveTestSession(dal.GetDB());

            //Assert
            Assert.IsFalse(controller.VM.TestEnvironment, "path that does not begin with localhost should result in test env flag of false.");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }


        [TestMethod]
        public void ActionExecutingTest_UserLoggedIn()
        {
            //Arrange
            var dal = DTTestOrganizer.DAL_LIVE();

            //Act
            var controller = ExecuteWithLoggedInUser(dal, DTTestConstants.TestRemoteHost);

            //Clean up db - test user is left in place
            RemoveTestSession(dal.GetDB());
            var corsFlag = controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.IsNotNull(controller.VM, "Controller's VM not set.");
            Assert.IsNotNull(controller.VM.User, "User not set on VM.");
            Assert.AreEqual(controller.VM.User.email, DTTestConstants.TestUserEmail, "User set incorrectly");
            Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }

        [TestMethod]
        public void ActionExecutingTest_SessionInPostData()
        {
            //Arrange
            var dal = DTTestOrganizer.DAL_LIVE();
            var config = DTTestOrganizer.InitConfiguration();
            var controller = DTTestOrganizer.InitializeDTController(dal.GetDB(), true);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.TestRemoteHost);    
            httpContext.Request.Headers.Add(HeaderNames.ContentType, new StringValues("application/x-www-form-urlencoded"));
            var form = new Dictionary<string, StringValues>();
            StringValues val = new StringValues(DTTestConstants.TestSessionId);
            form["sessionId"] = val;
            httpContext.Request.Form = new FormCollection(form);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            ctx.ActionArguments["sessionId"] = DTTestConstants.TestSessionId;
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, dal);

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
            var dal = DTTestOrganizer.DAL_LIVE();
            var config = DTTestOrganizer.InitConfiguration();
            var controller = DTTestOrganizer.InitializeDTController(dal.GetDB(), true);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.TestRemoteHost);
            httpContext.Request.QueryString = new QueryString("?sessionId=" + DTTestConstants.TestSessionId);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, dal);

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
            var dal = DTTestOrganizer.DAL_LIVE();
            var config = DTTestOrganizer.InitConfiguration();
            var controller = DTTestOrganizer.InitializeDTController(dal.GetDB(), true, "", dal);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.TestRemoteHost);
            httpContext.Request.QueryString = new QueryString("?sessionId=" + Guid.Empty.ToString());
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, dal);

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
            var dal = DTTestOrganizer.DAL_LIVE();
            var testUser = dal.user(new DGDAL_Email() { Email = DTTestConstants.TestUserEmail });

            //Remove any session for this user
            var testSession = dal.session(testUser.id);

            //Set up context
            var httpContext = new DefaultHttpContext();
            var requestFeature = new HttpRequestFeature();
            var featureCollection = new FeatureCollection();
            requestFeature.Headers = new HeaderDictionary();
            requestFeature.Headers.Add(HeaderNames.Cookie, new StringValues(DTTestConstants.TestSessionId + "=" + DTTestConstants.TestSessionId));
            requestFeature.Headers.Add(HeaderNames.ContentType, new StringValues("application/x-www-form-urlencoded"));
            featureCollection.Set<IHttpRequestFeature>(requestFeature);
            var cookiesFeature = new RequestCookiesFeature(featureCollection);
            httpContext.Request.Cookies = cookiesFeature.Cookies;
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(DTTestConstants.TestRemoteHostAddress);

            //Set up controller
            var config = DTTestOrganizer.InitConfiguration();
            var logger = DTTestOrganizer.InitLogger();
            var controller = new DTController(config, logger, dal);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), controller);
            controller.ControllerContext = new ControllerContext(actionContext);
            var actionFilter = new DTAuthenticate(config, dal);

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
