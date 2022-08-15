using DanTech.Data;
using DanTechTests.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Linq;
using DanTech.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using AutoMapper;
using DanTech.Models.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace DanTechTests.Controllers
{
    [TestClass]
    public class PlannerTests
    {
        private static dgdb _db = null;
        private IConfiguration _config = DTTestConstants.InitConfiguration();
        private PlannerController _controller = null;
        private dtUser _testUser = null;
        // Valid values for tests
        private int _numberOfPlanItems = 4;

        public PlannerTests()
        {
            _db = DTTestConstants.DB();
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();

            var logger = factory.CreateLogger<PlannerController>();
            _controller = new PlannerController(_config, logger, _db);
            _testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var testSession = (from x in _db.dtSessions where x.user == _testUser.id select x).FirstOrDefault();
            if (testSession == null)
            {
                testSession = new dtSession() { user = _testUser.id, hostAddress = DTTestConstants.TestRemoteHostAddress };
                _db.dtSessions.Add(testSession);
            }
            testSession.expires = DateTime.Now.AddDays(1);
            testSession.session = DTTestConstants.TestSessionId;
            _controller.VM = new DanTech.Models.DTViewModel();
            _controller.VM.User = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtUser, dtUserModel>(); })).Map<dtUserModel>(_testUser);
            _db.SaveChanges();
        }

        private void SetControllerQueryString()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Host = new HostString(DTTestConstants.TestRemoteHost);
            httpContext.Request.QueryString = new QueryString("?sessionId=" + DTTestConstants.TestSessionId);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
            actionContext.ActionDescriptor = new ControllerActionDescriptor();
            _controller.ControllerContext = new ControllerContext(actionContext);
        }

        [TestMethod]
        public void ControllerInitialized()
        {
            Assert.IsNotNull(_controller, "Planner controller not correctly initialized.");
        }

        [TestMethod]
        public void PlanItemGet()
        {
            //Arrange
            _numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id select x).ToList().Count + 1;
            SetControllerQueryString();

            // Act
            var jsonRes = _controller.SetPlanItem(DTTestConstants.TestValue, null, null, null, null, null);
            var jsonGet = _controller.PlanItems(DTTestConstants.TestSessionId);

            // Assert
            Assert.AreEqual(((List<dtPlanItemModel>) jsonRes.Value).Count, _numberOfPlanItems, "Did not add test plan item correctly.");
            Assert.AreEqual(((List<dtPlanItemModel>)jsonGet.Value).Count, _numberOfPlanItems, "Did not retrieve plan items correctly.");
        }

        [TestMethod]
        public void Stati()
        {
            //Arrange
            int numberStati = (from x in _db.dtStatuses select x).ToList().Count;
            SetControllerQueryString();

            //Act
            var res = _controller.Stati(DTTestConstants.TestSessionId);

            //Assert
            Assert.AreEqual(((List<dtStatusModel>)res.Value).Count, numberStati, "Stati numbers don't match.");

        }
    }
}
