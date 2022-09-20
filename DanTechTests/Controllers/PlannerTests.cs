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
        }

        [TestMethod]
        public void ControllerInitialized()
        {
            Assert.IsNotNull(_controller, "Planner controller not correctly initialized.");
        }
        
        [TestMethod]
        public void PlanItem_AllRecurrences()
        {
            // This tests the ability to retrieve recurrences through the api
            // We are going to set four recurrences.
            // #1 is dated twenty days ago, and is an everyday recurrence. It should add 30 children.
            // #2 is dated ten days ago, and is a T/Th recurrence.
            //  It should add 8 plus an additional child if today is Sunday, Monday, Wednesday, or Thursday
            //  Or two if today is Tuesady
            // #3 is dated three days ago, and is a 1st & 15th recurrence. This should generate 2 children.
            // #4 is dated today and is an everyday recurrence,. It should add 30 children.
            // We then get the plan items.
            // The we get the recurrence items.

            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && x.recurrence.HasValue && x.recurrence.Value > 0 select x).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " for Getting Recurrences ";
            int numberOfChildren = 30+8+2+30;
            if (today.DayOfWeek >= DayOfWeek.Sunday && today.DayOfWeek <= DayOfWeek.Thursday) numberOfChildren++;
            if (today.DayOfWeek == DayOfWeek.Tuesday) numberOfChildren++;
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey + " #1", null, DateTime.Now.AddDays(-20).ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, (int) DtRecurrence.Daily_Weekly, null);
            res = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey + " #2", null, DateTime.Now.AddDays(-10).ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, (int) DtRecurrence.Daily_Weekly, "--*-*--");
            res = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey + " #3", null, DateTime.Now.AddDays(-3).ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, (int) DtRecurrence.Monthly, "1,15");
            res = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey + " #4", null, DateTime.Now.ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            res = _controller.PlanItems(DTTestConstants.TestSessionId);
            var recurrenceList = (List<dtPlanItemModel>) _controller.PlanItems(DTTestConstants.TestSessionId, 1, false, true, null, true).Value;

            //Assert
            Assert.AreEqual(numberOfPlanItems + 4, recurrenceList.Count, "Did not get the expected recurrence list.");
        }

        [TestMethod]
        public void PlanItem_Get()
        {
            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var totalPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id select x).ToList();
            var totalPlanItemsCurrent = (from x in _db.dtPlanItems where x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null)) select x).ToList();
            var numberOfNotCompletedPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && x.completed == null && (x.recurrence == null  || x.day >= today) select x).ToList();
            SetControllerQueryString();

            // Act
            var getResults = (List<dtPlanItemModel>) _controller.PlanItems(DTTestConstants.TestSessionId).Value;
            var getWithCompleted = (List<dtPlanItemModel>) _controller.PlanItems(DTTestConstants.TestSessionId, null, true).Value;

            List<int> missing = new List<int>();
            dtMisc log = new dtMisc() { title = "Test Results" };
            foreach (var i in getWithCompleted)
            {
                if (totalPlanItemsCurrent.Where(x => x.id == i.id).FirstOrDefault() == null)
                {
                    missing.Add(i.id.Value);
                }
            }

            // Assert
            Assert.AreEqual(getResults.Count, numberOfNotCompletedPlanItems.Count, "Did not retrieve plan items correctly.");
            Assert.AreEqual(getWithCompleted.Count, totalPlanItemsCurrent.Count, "Did not retrieve completed plan items correctly.");
        }

        [TestMethod]
        public void PlanItemSet()
        {
            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var totalPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id select x).ToList().Count;
            var totalPlanItemsCurrent = (from x in _db.dtPlanItems where x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null)) select x).ToList().Count;
            var numberOfNotCompletedPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && ((x.completed.HasValue == false || x.completed.Value == false) || x.day >= today) select x).ToList().Count;
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
        public void PlanItem_Delete()
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
        public void PlanItem_RecurrenceNotCurrent()
        {
            // If a recurrence is set for a previous date, when the plan items are retrieved, there should be items populated for the next 30 days.

            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed==null)) select x).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " for  Out of Date Recurrence Test";
            var testProj = (from x in _db.dtProjects where x.user == _testUser.id select x).FirstOrDefault();  //Just use the first project
            var targDate = DateTime.Now.AddDays(-50);
            DayOfWeek weekdayToday = DateTime.Now.DayOfWeek;
            int numberOfChildrenExpected = weekdayToday >= DayOfWeek.Monday && weekdayToday <= DayOfWeek.Thursday ? 9 : 8;
            SetControllerQueryString();

            //Act
            var jsonSetResult = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, targDate.ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, DTTestConstants.RecurrenceId_Daily, "--*-*--");
            var itemList = (List<dtPlanItemModel>)_controller.PlanItems(DTTestConstants.TestSessionId, null, true).Value;
            Console.WriteLine(itemList.Count);

            //Assert
            Assert.AreEqual(itemList.Count, numberOfPlanItems + numberOfChildrenExpected, "When getting the plan items, it should have populated up the number of items to include the expected number of children plus 1 for the recurrence.");
        }

        [TestMethod]
        public void PlanItemSet_DailyRecurrence_TTh_Filter()
        {
            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null)) select x).ToList().Count;
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
        public void PlanItemSet_Monthly_nth_Monday_past()
        {
            // Setting a recurrence with a 3 week cycle on M & F => 3:-*---*-. We are setting the start date equal to 14 days previous to today.
            // This means that we are beginning the 3rd week, and we expect to see entries on the next Monday and Friday, and then again in 3 weeks.

            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && x.day >= today select x).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " past recurrence with 3rd Monday & Wednesday of month Recurrence";
            //Most of the time we expect 30 days ahead to generate 3 M-F items. The exception is if today is a Tuesday.
            int expectedChildren = 2;
            SetControllerQueryString();

            //Act
            var setRes = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, today.AddDays(-14).ToShortDateString(), "13:00", null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Monthly_nth_day, "3:-*-*---");
            var recurrence = (from x in _db.dtPlanItems where x.user == _testUser.id && x.title == planItemKey && x.recurrence.HasValue && x.recurrence.Value == (int)DtRecurrence.Monthly_nth_day select x).FirstOrDefault();
            var children = (from x in _db.dtPlanItems where x.user == _testUser.id && x.parent.HasValue && x.parent.Value == recurrence.id select x).ToList();

            //Assert
            Assert.IsNotNull(recurrence, "Recurrence not set.");
            Assert.AreEqual(expectedChildren, children.Count, "Did not propogate correctly.");
            Assert.IsTrue(children[0].day.DayOfWeek == DayOfWeek.Monday || children[0].day.DayOfWeek == DayOfWeek.Wednesday, "Day of week incorrect.");
        }

        [TestMethod]
        public void PlanItemSet_MonthlyRecurrence()
        {
            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null)) select x).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " set item through API with 1st & 15th Recurrence";
            string dayOfMonthTargets = "1,15";
            //We expect to generate 2 children.
            int numberOfChildrenExpected = 2;
            var dateAfter30days = DateTime.Now.AddDays(30);
            // 30 day month will have an additional child if today is the 1st or 15th
            if (dateAfter30days.Day == DateTime.Now.Day && (DateTime.Now.Day == 1 || DateTime.Now.Day == 15)) numberOfChildrenExpected++;
            // Feb has just 28 days. So Jan 30 & Jan 31 & Feb 1 all have an extra, as do Feb 13-15
            if ((DateTime.Now.Month == 1 && (DateTime.Now.Day == 30 || DateTime.Now.Day == 31)) ||
                (DateTime.Now.Month == 2 && (DateTime.Now.Day == 1 || DateTime.Now.Day == 13 || DateTime.Now.Day == 14 || DateTime.Now.Day == 15)))
            {
                numberOfChildrenExpected++;
            }
            SetControllerQueryString();

            //Act
            var jsonSetRes = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, null, null, null, null, null, null, null, null, null, null, true, null, null, null, (int) DtRecurrence.Monthly, dayOfMonthTargets);
            var returnedList = (List<dtPlanItemModel>)jsonSetRes.Value;
            var setItem = (from x in _db.dtPlanItems where x.title == planItemKey && x.recurrence == (int)DtRecurrence.Monthly select x).FirstOrDefault();
            var childItemFor1st = returnedList.Where(x => x.day.Day == 1 && x.parent.HasValue && x.parent.Value == setItem.id).FirstOrDefault();
            var childItemFor15th = returnedList.Where(x => x.day.Day == 15 && x.parent.HasValue && x.parent.Value == setItem.id).FirstOrDefault();

            //Assert
            Assert.IsNotNull(setItem, "Did not set the recurrance.");
            Assert.AreEqual(returnedList.Count, numberOfPlanItems + numberOfChildrenExpected + 1, "Setting the recurring plan item should have increased number of plan items by 1 and the expected number of children..");
            Assert.IsNotNull(childItemFor1st, "No item set for 1st.");
            Assert.IsNotNull(childItemFor15th, "No item set for 15th.");
        }

        [TestMethod]
        public void PlanItem_SemiMonthlyRecurrence()
        {
            // Setting a recurrence with a 3 week cycle on M & F => 3:-*---*-. We are setting the start date equal to 14 days previous to today.
            // This means that we are beginning the 3rd week, and we expect to see entries on the next Monday and Friday, and then again in 3 weeks.

            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && x.day >= today select x).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " recurrence with 3 weeks MF Recurrence";
            //Most of the time we expect 30 days ahead to generate 3 M-F items. The exception is if today is a Tuesday.
            int expectedChildren = (DateTime.Now.DayOfWeek == DayOfWeek.Monday || DateTime.Now.DayOfWeek == DayOfWeek.Tuesday) ? 3 : 2;
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, today.AddDays(-14).ToShortDateString(), "13:00", null, null, null, null, null, null, null, null, null, null, null, null, (int)DtRecurrence.Semi_monthly, "3:-*---*-");
            var recurrence = (from x in _db.dtPlanItems where x.user == _testUser.id && x.recurrence.HasValue && x.title == planItemKey select x).FirstOrDefault();
            var children = (from x in _db.dtPlanItems where x.parent.HasValue && x.parent.Value == recurrence.id select x).ToList();

            //Assert
            Assert.IsNotNull(res, "Recurrence not saved.");
            Assert.AreEqual(children.Count, expectedChildren, "Unexpected number of children.");
        }

        [TestMethod]
        public void PlanItem_SemiMonthlyRecurrence_Future()
        {
            // Setting a recurrence with a 3 week cycle on M & F => 3:-*---*-. We are setting the start date equal to 14 days in the future.
            // This means that it is 2 weeks until the beginning the 3rd week, and we expect to see entries on the next Monday and Friday after,
            // but the next cycle is 35 days aways, so there should be only these two.

            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && x.day >= today select x).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " future recurrence with 3 weeks MF Recurrence";
            //For this test we always expect 2 children to be populated. By setting the start date 14 days ahead, the next M & F including the
            // possible start should be in the 30 day range that is populated, but  the next cycle would be at day 35-41.
            int expectedChildren = 2;

            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, today.AddDays(14).ToShortDateString(), "13:00", null, null, null, null, null, null, null, null, null, null, null, null, (int)DtRecurrence.Semi_monthly, "3:-*---*-");
            var recurrence = (from x in _db.dtPlanItems where x.user == _testUser.id && x.recurrence.HasValue && x.title == planItemKey select x).FirstOrDefault();
            var children = (from x in _db.dtPlanItems where x.parent.HasValue && x.parent.Value == recurrence.id select x).ToList();

            //Assert
            Assert.IsNotNull(res, "Recurrence not saved.");
            Assert.AreEqual(children.Count, expectedChildren, "Unexpected number of children.");
            Assert.IsTrue(children[0].start.Value >= recurrence.start.Value, "Child should not start before the recurrence.");

        }

        [TestMethod]
        public void PlanItem_SetWithDailyRecurrence()
        {
            //Arrange
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfNotCompletedPlanItems = (from x in _db.dtPlanItems where x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null)) select x).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " set with Recurrence";
            SetControllerQueryString();

            //Act
            var jsonSetRes = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, null, null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            var returnedList = (List<dtPlanItemModel>)jsonSetRes.Value;

            //Assert
            Assert.AreEqual(returnedList.Count, numberOfNotCompletedPlanItems + 31, "Setting the recurring plan item should have increased number of plan items by 1 and one for each of the next 30 days.");
        }

        [TestMethod]
        public void Propagate_FromChild()
        {
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            string planItemKey = DTTestConstants.TestValue + " for propagate_fromchild";
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, null, null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            var recurrenceItem = (from x in _db.dtPlanItems where x.user == _testUser.id && x.title == planItemKey && x.recurrence.HasValue && x.recurrence.Value == (int)DtRecurrence.Daily_Weekly select x).FirstOrDefault();
            string recurrenceItemTitle = recurrenceItem.title;
            string recurrenceItemNote = recurrenceItem.note;
            var children = (from x in _db.dtPlanItems where x.parent.HasValue && x.parent.Value == recurrenceItem.id select x).ToList();
            string childrenTitle = children[0].title;
            string childrenNote = children[0].note;
            // Change note, and propagate.
            var changeRes = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey + " changed.", "Note set", null, null, null, null, null, null, null, null, null, null, true, null, null, children[0].id, null, null);
            var propRes = _controller.Propagate(DTTestConstants.TestSessionId, children[0].id);
            recurrenceItem = (from x in _db.dtPlanItems where x.user == _testUser.id && x.title == (planItemKey + " changed.") && x.recurrence.HasValue && x.recurrence.Value == (int)DtRecurrence.Daily_Weekly select x).FirstOrDefault();
            children = (from x in _db.dtPlanItems where x.parent.HasValue && x.parent.Value == recurrenceItem.id select x).ToList();

            //Assert
            Assert.AreEqual(recurrenceItemTitle, planItemKey, "Recurrence item not correctly set.");
            Assert.AreEqual(children.Count, 30, "Should have been 30 children.");
            Assert.AreEqual(childrenTitle, planItemKey, "Child items not correctly set.");
            Assert.AreEqual(recurrenceItem.title, planItemKey + " changed.", "Recurrence item not updated correctly.");
            Assert.AreEqual(recurrenceItem.note, "Note set", "Recurrence item note not updated correctly.");
            Assert.AreEqual(children[0].title, planItemKey + " changed.", "Child item not updated correctly.");
            Assert.AreEqual(children[0].note, "Note set", "Child item note not updated correctly.");
            Assert.AreEqual(children[29].title, planItemKey + " changed.", "Last child item not updated correctly.");
            Assert.AreEqual(children[29].note, "Note set", "Last child item note not updated correctly.");
        }

        [TestMethod]
        public void Propagate_Recurrence()
        {
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            string planItemKey = DTTestConstants.TestValue + " for propagate_recurrence";
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey, null, null, null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            var recurrenceItem = (from x in _db.dtPlanItems where x.user == _testUser.id && x.title == planItemKey && x.recurrence.HasValue && x.recurrence.Value == (int)DtRecurrence.Daily_Weekly select x).FirstOrDefault();
            string recurrenceItemTitle = recurrenceItem.title;
            string recurrenceItemNote = recurrenceItem.note;
            var children = (from x in _db.dtPlanItems where x.parent.HasValue && x.parent.Value == recurrenceItem.id select x).ToList();
            string childrenTitle = children[0].title;
            string childrenNote = children[0].note;
            // Change note, and propagate.
            var changeRes = _controller.SetPlanItem(DTTestConstants.TestSessionId, planItemKey + " changed.", "Note set", null, null, null, null, null, null, null, null, null, null, true, null, null, recurrenceItem.id, (int)DtRecurrence.Daily_Weekly, null);
            var propRes = _controller.Propagate(DTTestConstants.TestSessionId, recurrenceItem.id);
            recurrenceItem = (from x in _db.dtPlanItems where x.user == _testUser.id && x.title == (planItemKey + " changed.") && x.recurrence.HasValue && x.recurrence.Value == (int)DtRecurrence.Daily_Weekly select x).FirstOrDefault();
            children = (from x in _db.dtPlanItems where x.parent.HasValue && x.parent.Value == recurrenceItem.id select x).ToList();

            //Assert
            Assert.AreEqual(recurrenceItemTitle, planItemKey, "Recurrence item not correctly set.");
            Assert.AreEqual(children.Count, 30, "Should have been 30 children.");
            Assert.AreEqual(childrenTitle, planItemKey, "Child items not correctly set.");
            Assert.AreEqual(recurrenceItem.title, planItemKey + " changed.", "Recurrence item not updated correctly.");
            Assert.AreEqual(recurrenceItem.note, "Note set", "Recurrence item note not updated correctly.");
            Assert.AreEqual(children[0].title, planItemKey + " changed.", "Child item not updated correctly.");
            Assert.AreEqual(children[0].note, "Note set", "Child item note not updated correctly.");
            Assert.AreEqual(children[29].title, planItemKey + " changed.", "Last child item not updated correctly.");
            Assert.AreEqual(children[29].note, "Note set", "Last child item note not updated correctly.");
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
            var projectsWithNewItem = _controller.SetProject(DTTestConstants.TestSessionId, newProj.title, newProj.shortCode, newProj.status, newProj.colorCode ?? 0, newProj.priority, newProj.sortOrder, newProj.notes);
            var projectsWithUpdatedItem = _controller.SetProject(DTTestConstants.TestSessionId, copyOfExisting.title, copyOfExisting.shortCode, copyOfExisting.status, copyOfExisting.colorCode ?? 0, copyOfExisting.priority, copyOfExisting.sortOrder, copyOfExisting.notes);
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
    }
}
