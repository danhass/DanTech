using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using DanTech.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DanTech.Services;
using System.Threading.Tasks;
using DanTech.Data;
using AutoMapper;
using DanTech.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using K4os.Hash.xxHash;
using System;
using DTUserManagement.Services;
using System.Net;
using System.Collections.Generic;

namespace DanTechTests.Controllers
{
    [TestClass]
    public class AdminTests
    {
        private IConfiguration _config = DTTestOrganizer.InitConfiguration();
        private AdminController _controller = null;
        private static dtUser _testUser = null;

        public AdminTests()
        {            
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();

            var logger = factory.CreateLogger<AdminController>();
            var dbctx = new dtdb(_config.GetConnectionString("DG"));
            var db = new DTDBDataService(_config, dbctx);
            _controller = new AdminController(_config, logger, db, dbctx);
            _testUser = DTTestOrganizer.TestUser;
            if (_controller != null)
            {
                var testSession = DTTestOrganizer.TestUserSession;
                _controller.VM = new DanTech.Data.Models.DTViewModel();
                _controller.VM.User = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtUser, dtUserModel>(); })).Map<dtUserModel>(_testUser);
            }

        }

        private void SetControllerQueryString(string sessionId = "")
        {
            if (string.IsNullOrEmpty(sessionId)) sessionId = DTTestOrganizer.TestSession.session;
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.TestRemoteHost);
            httpContext.Connection.RemoteIpAddress = IPAddress.Parse(DTTestConstants.LocatHostIP);
            httpContext.Request.QueryString = new QueryString("?sessionId=" + sessionId);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            _controller.ControllerContext = new ControllerContext(actionContext);
        }

        [TestMethod]
        public void AdminController_InstantiateDefault()
        {
            Assert.IsNotNull(_controller, "Did not instantiate admin controller.");
        }

        [TestMethod]
        public void AdminController_Index()
        {
            //Act
            var res = _controller.Index();

            //Assert
            Assert.IsNotNull(res, "Could not instantiate admin index view");
        }        
        [TestMethod]
        public void AdminController_SetPW()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var usr = db.Users.Where(x => x.id == _testUser.id).FirstOrDefault();
            var session = db.Sessions.Where(x => x.user == usr.id).FirstOrDefault();
            SetControllerQueryString(session.session);

            //Act
            _controller.SetPW(session.session, "123TEST321");

            //Assert
            dbctx.Entry(usr).Reload();
            Assert.AreEqual(usr.pw, "123TEST321");
        }

        [TestMethod]
        public void AdminController_SetDoNotSetPWFlag()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var usr = db.Users.Where(x => x.id == _testUser.id).FirstOrDefault();
            usr.pw = "123DNSFlagTest321";
            usr.doNotSetPW = null;
            db.Set(usr);
            var session = db.Sessions.Where(x => x.user == usr.id).FirstOrDefault();
            SetControllerQueryString(session.session);

            //Act
            _controller.SetOrClearDoNotSetPWFlag(session.session, true);

            //Assert
            dbctx.Entry(usr).Reload();
            Assert.IsNull(usr.pw);
            Assert.IsTrue(usr.doNotSetPW);
        }

        [TestMethod]
        public void AdminController_ClearDoNotSetPWFlag()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var usr = db.Users.Where(x => x.id == _testUser.id).FirstOrDefault();
            usr.pw = null;
            usr.doNotSetPW = true;
            db.Set(usr);
            var session = db.Sessions.Where(x => x.user == usr.id).FirstOrDefault();
            SetControllerQueryString(session.session);

            //Act
            _controller.SetOrClearDoNotSetPWFlag(session.session, false);

            //Assert
            dbctx.Entry(usr).Reload();
            Assert.IsNull(usr.doNotSetPW);
        }
        [TestMethod]
        public void AdminController_CompleteRegistration()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var svc = new DTRegistration(db);
            svc.SetConfig(_config);
            var regKey = svc.RegistrationKey();
            string email = "complete_reg_" + DTTestConstants.TestUserEmail;
            dtRegistration reg = new() { email = email, regKey = regKey, created = DateTime.Now.AddHours(-1) };
            reg = db.Set(reg);
            SetControllerQueryString();

            //Act
            var result = _controller.CompleteRegistration(email, regKey);

            //Assert
            var usr = db.Users.Where(x => x.email == email).FirstOrDefault();
            var session = db.Sessions.Where(x => x.user == usr.id).FirstOrDefault();
            var test = result.Value.ToString().Split("=")[1].Replace("}", "").Trim();
            Assert.IsNotNull(result);
            Assert.IsNotNull(usr);
            Assert.IsNotNull(session);
            Assert.AreEqual(test, session.session);

            //Cleanup
            if (session != null) db.Delete(session);
            if (usr != null) db.Delete(usr);
        }
    }
}
