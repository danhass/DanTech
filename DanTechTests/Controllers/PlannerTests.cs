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
                testSession.expires = DateTime.Now.AddDays(1);
                testSession.session = DTTestConstants.TestSessionId;
                _db.dtSessions.Add(testSession);
                _db.SaveChanges();
            }
            _controller.VM = new DanTech.Models.DTViewModel();
            _controller.VM.User = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtUser, dtUserModel>(); })).Map<dtUserModel>(_testUser);
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
        public void PlanItemSet()
        {
            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var totalPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id select x).ToList().Count;
            var totalPlanItemsCurrent = (from x in _db.dtPlanItems where x.user == _testUser.id && x.day >= today select x).ToList().Count;
            var numberOfNotCompletedPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && (x.completed.HasValue == false || x.completed.Value == false) && x.day >= today select x).ToList().Count;
            SetControllerQueryString();

            // Act
            var jsonRes = _controller.SetPlanItem(DTTestConstants.TestSessionId, DTTestConstants.TestValue, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
            var returnedList = (List<dtPlanItemModel>) jsonRes.Value;
            var testItem = returnedList.Where(x => x.title == DTTestConstants.TestValue).FirstOrDefault();
            var jsonResCompleted = _controller.SetPlanItem(DTTestConstants.TestSessionId, testItem.title, DTTestConstants.TestValue2, null, null, null, null, null, null, true, null, null, null, true, null, null, testItem.id);
            var returnedListFromCompleted = (List<dtPlanItemModel>)jsonResCompleted.Value;
            var completedTestItem = returnedListFromCompleted.Where(x => x.title == DTTestConstants.TestValue && x.note == DTTestConstants.TestValue2).FirstOrDefault();

            // Assert
            Assert.AreEqual(returnedList.Count, totalPlanItemsCurrent + 1, "Did not add test plan item correctly.");
            Assert.AreEqual(testItem.title, DTTestConstants.TestValue, "Test item title incorrect.");
            Assert.AreEqual(completedTestItem.title, DTTestConstants.TestValue, "Completed test item wrong title.");
            Assert.AreEqual(completedTestItem.note, DTTestConstants.TestValue2, "Note not set.");
            Assert.IsTrue(completedTestItem.completed.Value);
        }

        [TestMethod]
        public void PlanItem_RecurrenceNotCurrent()
        {
            // If a recurrence is set for a previous date, when the plan items are retrieved, there should be items populated for the next 30 days.

            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && x.day >= today select x).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " for  Out of Date Recurrence Test";
            var testProj = (from x in _db.dtProjects where x.user == _testUser.id select x).FirstOrDefault();  //Just use the first project
            var targDate = DateTime.Now.AddDays(-50);
            DayOfWeek weekdayToday = DateTime.Now.DayOfWeek;
            int numberOfChildrenExpected = weekdayToday >= DayOfWeek.Monday && weekdayToday <= DayOfWeek.Thursday ? 9 : 8;
            SetControllerQueryString();

            //Act
            var jsonSetResult = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, targDate.ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, DTTestConstants.RecurrenceId_Daily, "--*-*--");
            var itemList = (List<dtPlanItemModel>) _controller.PlanItems(DTTestConstants.TestSessionId, null, true).Value;
            Console.WriteLine(itemList.Count);

            //Assert
            Assert.AreEqual(itemList.Count, numberOfPlanItems + numberOfChildrenExpected, "When getting the plan items, it should have populated up the number of items to include the expected number of children plus 1 for the recurrence.");
        }

        [TestMethod]
        public void PlanItemSetWithDailyRecurrence()
        {
            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfNotCompletedPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && x.day >= today select x).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " set with Recurrence";
            var testProj = (from x in _db.dtProjects where x.user == _testUser.id select x).FirstOrDefault();  //Just use the first project
            SetControllerQueryString();

            //Act
            var jsonSetRes = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, null, null, null, null, null, null, null, null, testProj.id, null, true, null, null, null, DTTestConstants.RecurrenceId_Daily, null);
            var returnedList = (List<dtPlanItemModel>)jsonSetRes.Value;

            //Assert
            Assert.AreEqual(returnedList.Count, numberOfNotCompletedPlanItems + 31, "Setting the recurring plan item should have increased number of plan items by 1 and one for each of the next 30 days.");
        }

        [TestMethod] 
        public void PlanItemSet_DailyRecurrence_TTh_Filter()
        {
            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && x.day >= today select x).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " set item through API with TTh Recurrence";
            //Most of the time we expect 30 days ahead to generate 8 T-Th unless we are M, T, W, or Th, then the extra 2 days will add a T-Th
            DayOfWeek weekdayToday = DateTime.Now.DayOfWeek;
            int numberOfChildrenExpected = weekdayToday >= DayOfWeek.Monday && weekdayToday <= DayOfWeek.Thursday ? 9 : 8;
            SetControllerQueryString();

            //Act
            var jsonSetRes = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, null, null, null, null, null, null, null, null, null, null, true, null, null, null, DTTestConstants.RecurrenceId_Daily, "--*-*--");            
            var returnedList = (List<dtPlanItemModel>)jsonSetRes.Value;

            //Assert
            Assert.AreEqual(returnedList.Count, numberOfPlanItems + numberOfChildrenExpected + 1, "Setting the recurring plan item should have increased number of plan items by 1 and the expected number of children..");
        }

        [TestMethod]
        public void PlanItemDelete()
        {
            //Arrange
            _numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id select x).ToList().Count + 1;
            SetControllerQueryString();
            string key = DTTestConstants.TestValue + " Delete Test";

            // Act
            var jsonRes = _controller.SetPlanItem(DTTestConstants.TestSessionId, key, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
            var itemSetCt = (from x in _db.dtPlanItems where x.title == key select x).ToList().Count;
            var setItem = (from x in _db.dtPlanItems where x.title == key select x).FirstOrDefault();
            var delRes = _controller.DeletePlanItem(DTTestConstants.TestSessionId, setItem.id);
            var delItemCt = (from x in _db.dtPlanItems where x.title == key select x).ToList().Count;

            //Assert
            Assert.AreEqual(itemSetCt, delItemCt + 1, "When set, there should be one more item counts than once deleted.");
            Assert.IsTrue((bool)delRes.Value, "Controller should have confirmed delete.");
        }

        [TestMethod]
        public void PlanItem_Get()
        {
            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var totalPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id select x).ToList().Count;
            var totalPlanItemsCurrent = (from x in _db.dtPlanItems where x.user == _testUser.id && x.day >= today select x).ToList().Count;
            var numberOfNotCompletedPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && (x.completed.HasValue == false || x.completed.Value == false) && x.day >= today select x).ToList().Count;
            SetControllerQueryString();

            // Act
            var jsonGet = _controller.PlanItems(DTTestConstants.TestSessionId);
            var jsonGetWithCompleted = _controller.PlanItems(DTTestConstants.TestSessionId, null, true);

            // Assert
            Assert.AreEqual(((List<dtPlanItemModel>)jsonGet.Value).Count, numberOfNotCompletedPlanItems, "Did not retrieve plan items correctly.");
            Assert.AreEqual(((List<dtPlanItemModel>)jsonGetWithCompleted.Value).Count, totalPlanItemsCurrent, "Did not retrieve completed plan items correctly.");
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
            var projectsWithNewItem = _controller.SetProject("", newProj.title, newProj.shortCode, newProj.status, newProj.colorCode ?? 0, newProj.priority, newProj.sortOrder, newProj.notes);
            var projectsWithUpdatedItem = _controller.SetProject("", copyOfExisting.title, copyOfExisting.shortCode, copyOfExisting.status, copyOfExisting.colorCode ?? 0, copyOfExisting.priority, copyOfExisting.sortOrder, copyOfExisting.notes);
//            var corsFlag = _controller.Response.Headers["Access-Control-Allow-Origin"];

            //Assert
            Assert.AreEqual(((List<dtProjectModel>)projectsWithNewItem.Value).Where(x => x.title.Contains("New_Test_Through_Controller")).ToList().Count, 1, "Should be one new project with the title showing it was created here.");
            Assert.AreEqual(((List<dtProjectModel>)projectsWithUpdatedItem.Value).Where(x => x.notes == "Updated by PlannerTests:SetProject").ToList().Count, 1, "Should be exactly one projected updated through this.");
 //           Assert.AreEqual(corsFlag, "*", "CORS flag not set");
        }

        [TestMethod]
        public void Recurrences()
        {
            //Arrange
            int numberRecurrences = (from x in _db.dtRecurrences select x).ToList().Count;
            SetControllerQueryString();

            //Act
            var res = _controller.Recurrences(DTTestConstants.TestSessionId);
            var firstRecurrence = ((List<dtRecurrenceModel>)res.Value)[0];
            var fisttRecurrenceInDB = (from x in _db.dtRecurrences where x.id == firstRecurrence.id select x).FirstOrDefault();

            //Assert
            Assert.AreEqual(((List<dtRecurrenceModel>)res.Value).Count, numberRecurrences, "Recurrence numbers don't match.");
            Assert.IsNotNull(fisttRecurrenceInDB, "Did not retrieve any recurrences.");
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
