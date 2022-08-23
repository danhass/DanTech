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
        private IConfiguration _config = DTTestOrganizer.InitConfiguration();
        private PlannerController _controller = null;
        private dtUser _testUser = null;
        // Valid values for tests
        private int _numberOfPlanItems = 4;

        public PlannerTests()
        {
            _db = DTTestOrganizer.DB();
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
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
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
            var jsonRes = _controller.SetPlanItem(DTTestConstants.TestSessionId, DTTestConstants.TestValue, null, null, null, null, null, null);
            var jsonGet = _controller.PlanItems(DTTestConstants.TestSessionId);

            // Assert
            Assert.AreEqual(((List<dtPlanItemModel>) jsonRes.Value).Count, _numberOfPlanItems, "Did not add test plan item correctly.");
            Assert.AreEqual(((List<dtPlanItemModel>)jsonGet.Value).Count, _numberOfPlanItems, "Did not retrieve plan items correctly.");
        }

        [TestMethod]
        public void SetProject()
        {
            //Arrange
            int numProjects = DTTestOrganizer._numberOfProjects;
            var testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var testStatus = (from x in _db.dtStatuses where x.title == DTTestConstants.TestStatus select x).FirstOrDefault();
            var allProjects = (from x in _db.dtProjects select x).OrderBy(x => x.id).ToList();
            var existingProject = allProjects[allProjects.Count - 1]; //The last three are test projects
            var copyOfExisting = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtProject, dtProject>(); })).Map<dtProject>(existingProject);
            copyOfExisting.notes = "Updated by PlannerTests:SetProject";
            var newProj = new dtProject()
            {
                notes = "new test item from PlannerTests:SetProject",
                shortCode = "TST",
                status = testStatus.id,
                title = DTTestConstants.TestProjectTitlePrefix + "New_Test_Through_Controller",
                user = testUser.id
            };
            SetControllerQueryString();

            //Act
            var projectsWithNewItem = _controller.SetProject("", newProj.title, newProj.shortCode, newProj.status, newProj.colorCode.HasValue ? newProj.colorCode.Value : 0, newProj.priority, newProj.sortOrder, newProj.notes);
            var projectsWithUpdatedItem = _controller.SetProject("", copyOfExisting.title, copyOfExisting.shortCode, copyOfExisting.status, copyOfExisting.colorCode.HasValue ? copyOfExisting.colorCode.Value : 0, copyOfExisting.priority, copyOfExisting.sortOrder, copyOfExisting.notes);
//            var corsFlag = _controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.AreEqual(((List<dtProjectModel>)projectsWithNewItem.Value).Where(x => x.title.Contains("New_Test_Through_Controller")).ToList().Count, 1, "Should be one new project with the title showing it was created here.");
            Assert.AreEqual(((List<dtProjectModel>)projectsWithUpdatedItem.Value).Where(x => x.notes == "Updated by PlannerTests:SetProject").ToList().Count, 1, "Should be exactly one projected updated through this.");
 //           Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }

        [TestMethod]
        public void Stati()
        {
            //Arrange
            int numberStati = (from x in _db.dtStatuses select x).ToList().Count;
            SetControllerQueryString();

            //Act
            var res = _controller.Stati(DTTestConstants.TestSessionId);
           //var corsFlag = _controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.AreEqual(((List<dtStatusModel>)res.Value).Count, numberStati, "Stati numbers don't match.");
            //Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }

        [TestMethod]
        public void ColorCodes()
        {
            //Arrange
            int numberColorCodes = (from x in _db.dtColorCodes select x).ToList().Count;
            SetControllerQueryString();

            //Act
            var res = _controller.ColorCodes(DTTestConstants.TestSessionId);
    //        var corsFlag = _controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.AreEqual(((List<dtColorCodeModel>)res.Value).Count, numberColorCodes, "Color codes numbers don't match.");
    //        Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }
    }
}
