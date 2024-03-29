﻿using DanTech.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using DanTech.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AutoMapper;
using DanTech.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Controllers;
using DanTech.Services;
using System.Threading.Tasks;
using System.Drawing.Text;
using System.Threading;

namespace DanTechTests.Controllers
{
    [TestClass]
    public class PlannerTests
    {
        private IConfiguration _config = DTTestOrganizer.InitConfiguration();
        private PlannerController _controller = null;
        private static dtUser _testUser = null;
        // Valid values for tests
        private int _numberOfPlanItems = 4;

        public PlannerTests()
        {
            var dbctx = new dtdb(_config.GetConnectionString("DG"));
            var db = new DTDBDataService(_config, dbctx);
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();
            var logger = factory.CreateLogger<PlannerController>();
            _testUser = DTTestOrganizer.TestUser;
            if (_controller == null)
            {
                _controller = new PlannerController(_config, logger, db, dbctx);
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
            httpContext.Request.QueryString = new QueryString("?sessionId=" + sessionId);
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor(), new ModelStateDictionary());
            _controller.ControllerContext = new ControllerContext(actionContext);
        }

        [TestMethod]
        public void ColorCodes()
        {

            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            int numberColorCodes = db.ColorCodes.Count;
            SetControllerQueryString();

            //Act
            var res = _controller.ColorCodes(DTTestOrganizer.TestSession.session);

            //Assert
            Assert.AreEqual(((List<dtColorCodeModel>)res.Value).Count, numberColorCodes, "Color codes numbers don't match.");
            
        }

        [TestMethod]
        public void ControllerInitialized()
        {
            
            Assert.IsNotNull(_controller, "Planner controller not correctly initialized.");
            
        }

        [TestMethod]
        public void PlanItem_Adjust()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            SetControllerQueryString();
            var projKey = DTTestConstants.TestValue + " Adjust Project";
            _controller.SetProject(DTTestOrganizer.TestSession.session, projKey, "TADJ", (int)DtStatus.Active);
            var proj = db.Projects.Where(x => x.title == projKey && x.user == _testUser.id).FirstOrDefault();
            //Three items. Each with 2 with one hour duration with a conflict. The their has no duration, so it has no conflict.
            //These should adjust so that the third is after the first, and the 2nd doesn't change.
            var key1 = DTTestConstants.TestValue + " Adjust #1";
            var start1 = DateTime.Now.AddMinutes(10).ToString("HH:mm");
            var end1 = DateTime.Now.AddMinutes(70).ToString("HH:mm");
            var key2 = DTTestConstants.TestValue + " Adjust #2";
            var start2 = DateTime.Now.AddMinutes(30).ToString("HH:mm");
            var end2 = DateTime.Now.AddMinutes(90).ToString("HH:mm");
            var key3 = DTTestConstants.TestValue + " Adjust #3";
            var start3 = DateTime.Now.AddMinutes(20).ToString("HH:mm");
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key1, null, DateTime.Now.ToShortDateString(), start1, null, end1, null, null, null, null, proj.id);
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key2, null, DateTime.Now.ToShortDateString(), start2, null, end2, null, null, null, null, proj.id);
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key3, null, DateTime.Now.ToShortDateString(), start3, null, null, null, null, null, null, proj.id);

            //Act
            _controller.Adjust(DTTestOrganizer.TestSession.session);

            //Assert
            var item1 = db.PlanItems.Where(x => x.title == key1).FirstOrDefault();
            var item2 = db.PlanItems.Where(x => x.title == key2).FirstOrDefault();
            var item3 = db.PlanItems.Where(x => x.title == key3).FirstOrDefault();
            Assert.IsTrue(item1.start.Value.AddMinutes(60) <= item2.start.Value, "Item 2 conflicts with item 1.");

            //Antiseptic
            db.Delete(new List<dtPlanItem>() { item1, item2, item3 });
            db.Delete(proj);   
        }

        [TestMethod]
        public void PlanItem_Adjust_PriorityFocused()
        {
            // Going to have five items.
            // #1 Starts at a fixed time 10 minutes with a 60 minute duration from now.
            // #2 Sub item startig in 15 munutes.
            // #3 Starts in 15 minutes with a 30 minute duration with a 1000 priority (default).
            // #4 Started 15 minutes ago with a 30 minute duration and a priority of 3000
            // #5 Starts in 20 minutes with an hour duration and  a 4000 priority.
            // The adjusted values should be: 
            // #1 -> start in 10
            // #2 -> start in 15 minutes
            // #5 -> start in 70 minutes
            // #4 -> start in 130 minutes
            // #3 -> start in 160 minutes


            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            SetControllerQueryString();
            var projKey = DTTestConstants.TestValue + " Adjust With Priority Project";
            _controller.SetProject(DTTestOrganizer.TestSession.session, projKey, "TAP", (int)DtStatus.Active);
            var proj = db.Projects.Where(x => x.title == projKey && x.user == _testUser.id).FirstOrDefault();
            //Three items. Each with 2 with one hour duration with a conflict. The their has no duration, so it has no conflict.
            //These should adjust so that the third is after the first, and the 2nd doesn't change.
            var now = DateTime.Now;
            var key1 = DTTestConstants.TestValue + " Adjust W/Priority #1";
            var start1 = now.AddMinutes(10).ToString("HH:mm");
            var end1 = now.AddMinutes(70).ToString("HH:mm");
            var key2 = DTTestConstants.TestValue + " Adjust W/Priority #2";
            var start2 = now.AddMinutes(15).ToString("HH:mm");
            var key3 = DTTestConstants.TestValue + " Adjust w/Priority #3";
            var start3 = now.AddMinutes(15).ToString("HH:mm");
            var end3 = now.AddMinutes(45).ToString("HH:mm");
            var key4 = DTTestConstants.TestValue + " Adjust w/Priority #4";
            var start4 = now.AddMinutes(-15).ToString("HH:mm");
            var end4 = now.AddMinutes(15).ToString("HH:mm");
            var key5 = DTTestConstants.TestValue + " Adjust w/Priority #5";
            var start5 = now.AddMinutes(20).ToString("HH:mm");
            var end5 = now.AddMinutes(80).ToString("HH:mm");
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key1, null, DateTime.Now.ToShortDateString(), start1, null, end1, null, null, null, null, proj.id, null, null, null, null, null, null, null, true);
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key2, null, DateTime.Now.ToShortDateString(), start2, null, null, null, null, null, null, proj.id);
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key3, null, DateTime.Now.ToShortDateString(), start3, null, end3, null, null, null, null, proj.id);
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key4, null, DateTime.Now.ToShortDateString(), start4, null, end4, 3000, null, null, null, proj.id);
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key5, null, DateTime.Now.ToShortDateString(), start5, null, end5, 5000, null, null, null, proj.id);

            //Act
            _controller.Adjust(DTTestOrganizer.TestSession.session);

            //Assert
            Assert.AreEqual(db.PlanItems.Where(x => x.title == key1 && x.project == proj.id).FirstOrDefault().start.Value.ToString("HH:mm"), now.AddMinutes(10).ToString("HH:mm"), "Item #1 start time is wrong.");
            Assert.AreEqual(db.PlanItems.Where(x => x.title == key2 && x.project == proj.id).FirstOrDefault().start.Value.ToString("HH:mm"), now.AddMinutes(15).ToString("HH:mm"), "Item #2 start time is wrong.");
            Assert.AreEqual(db.PlanItems.Where(x => x.title == key3 && x.project == proj.id).FirstOrDefault().start.Value.ToString("HH:mm"), now.AddMinutes(160).ToString("HH:mm"), "Item #3 start time is wrong.");
            Assert.AreEqual(db.PlanItems.Where(x => x.title == key4 && x.project == proj.id).FirstOrDefault().start.Value.ToString("HH:mm"), now.AddMinutes(130).ToString("HH:mm"), "Item #4 start time is wrong.");
            Assert.AreEqual(db.PlanItems.Where(x => x.title == key5 && x.project == proj.id).FirstOrDefault().start.Value.ToString("HH:mm"), now.AddMinutes(70).ToString("HH:mm"), "Item #5 start time is wrong.");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.project == proj.id).ToList());
            db.Delete(proj);            
        }

        [TestMethod]
        public void PlanItem_Adjust_With_Fixed()
        {
            // Going to have four items.
            // #1: Starting in 5 minutes and lasting an hour.
            // #2: fixed that starts in 20 minutes and lasts 30 minute.
            // #3: starts in 25 minutes with no duration -- so it is contained.
            // #4: starts in 30 minutes and lasts 30 minutes.
            // After adjustment, they should be lined up: #2->#3->#1->#4

            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            SetControllerQueryString();
            var projKey = DTTestConstants.TestValue + " Adjust Project";
            _controller.SetProject(DTTestOrganizer.TestSession.session, projKey, "TADJ", (int)DtStatus.Active);
            var proj = db.Projects.Where(x => x.title == projKey && x.user == _testUser.id).FirstOrDefault();
            //Three items. Each with 2 with one hour duration with a conflict. The their has no duration, so it has no conflict.
            //These should adjust so that the third is after the first, and the 2nd doesn't change.
            var key1 = DTTestConstants.TestValue + " Adjust With Fixed #1";
            var start1 = DateTime.Now.AddMinutes(5).ToString("HH:mm");
            var end1 = DateTime.Now.AddMinutes(65).ToString("HH:mm");
            var key2 = DTTestConstants.TestValue + " Adjust With Fixed #2";
            var start2Adj = DateTime.Now.AddMinutes(20);
            var start2 = start2Adj.ToString("HH:mm");
            var end2 = DateTime.Now.AddMinutes(50).ToString("HH:mm");
            var key3 = DTTestConstants.TestValue + " Adjust With Fixed #3";
            var start3Adj = DateTime.Now.AddMinutes(25);
            var start3 = start3Adj.ToString("HH:mm");
            var key4 = DTTestConstants.TestValue + " Adjust With Fixed #4";
            var start4 = DateTime.Now.AddMinutes(30).ToString("HH:mm");
            var end4 = DateTime.Now.AddMinutes(60).ToString("HH:mm");

            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key1, null, DateTime.Now.ToShortDateString(), start1, null, end1, null, null, null, null, proj.id);
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key2, null, DateTime.Now.ToShortDateString(), start2, null, end2, null, null, null, null, proj.id, null, null, null, null, null, null, null, true);
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key3, null, DateTime.Now.ToShortDateString(), start3, null, null, null, null, null, null, proj.id);
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key4, null, DateTime.Now.ToShortDateString(), start4, null, end4, null, null, null, null, proj.id);

            //Act
            _controller.Adjust(DTTestOrganizer.TestSession.session);
            var item1 = db.PlanItems.Where(x => x.title == key1).FirstOrDefault();
            var item2 = db.PlanItems.Where(x => x.title == key2).FirstOrDefault();
            var item3 = db.PlanItems.Where(x => x.title == key3).FirstOrDefault();
            var item4 = db.PlanItems.Where(x => x.title == key4).FirstOrDefault();

            //Assert
            Assert.AreEqual(item2.start.Value.ToString("HH:mm"), start2Adj.ToString("HH:mm"), "Fixed value not set correctly.");
            Assert.AreEqual(item3.start.Value.ToString("HH:mm"), start3Adj.ToString("HH:mm"), "0 duration not set correctly.");
            Assert.AreEqual(item1.start.Value.ToString("HH:mm"), start2Adj.AddMinutes(30).ToString("HH:mm"), "Adjustment did not set item 1 correctly.");
            Assert.AreEqual(item4.start.Value.ToString("HH:mm"), start2Adj.AddMinutes(90).ToString("HH:mm"), "Adjustment did not set item 4 correctly.");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.project == proj.id).ToList());
            db.Delete(proj);
            
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
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = db.PlanItems.Where(x => x.user == _testUser.id && x.recurrence.HasValue && x.recurrence.Value > 0).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " for Getting Recurrences ";
            string key1 = planItemKey + " #1";
            string key2 = planItemKey + " #2";
            string key3 = planItemKey + " #3";
            string key4 = planItemKey + " #4";
            int numberOfChildren = 30 + 8 + 2 + 30;
            if (today.DayOfWeek >= DayOfWeek.Sunday && today.DayOfWeek <= DayOfWeek.Thursday) numberOfChildren++;
            if (today.DayOfWeek == DayOfWeek.Tuesday) numberOfChildren++;
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey + " #1", null, DateTime.Now.AddDays(-20).ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey + " #2", null, DateTime.Now.AddDays(-10).ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Daily_Weekly, "--*-*--");
            res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey + " #3", null, DateTime.Now.AddDays(-3).ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Monthly, "1,15");
            res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey + " #4", null, DateTime.Now.ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            res = _controller.PlanItems(DTTestOrganizer.TestSession.session);
            var recurrenceList = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session, 1, false, true, null, true).Value;

            //Assert
            Assert.AreEqual(numberOfPlanItems + 4, recurrenceList.Count, "Did not get the expected recurrence list.");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => (x.title == key1 || x.title == key2 || x.title == key3 || x.title == key4) && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => (x.title == key1 || x.title == key2 || x.title == key3 || x.title == key4) && x.recurrence.HasValue).ToList());
            
        }

        [TestMethod]
        public void PlanItem_ColorStatus_Complete()
        {

            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            string key = DTTestConstants.TestValue + " Color: complete";
            var completeStatus = db.Stati.Where(x => x.id == (int)(DtStatus.Complete)).FirstOrDefault();
            var colorId = completeStatus.colorCode == null ? null : completeStatus.colorCode;
            var color = db.ColorCodes.Where(x => x.id == colorId).FirstOrDefault();
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, DateTime.Now.ToShortDateString(), DateTime.Now.AddMinutes(-40).ToString("HH:mm"), null, null, null, null, true, null, null, null, null, null, null, null, null, null);
            var items = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session, null, true).Value;
            var item = items.Where(x => x.title == key).FirstOrDefault();

            //Assert
            Assert.IsNotNull(item, "Did not set complete item");
            Assert.AreEqual(item.statusColor, color.title, "Status color not properly set");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key).ToList());
            
        }

        [TestMethod]
        public void PlanItem_ColorStatus_Conflict()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            string key = DTTestConstants.TestValue + " Color: conflict";
            var conflictStatus = db.Stati.Where(x => x.id == (int)(DtStatus.Conflict)).FirstOrDefault();
            var color = db.ColorCodes.Where(x => x.id == conflictStatus.colorCode).FirstOrDefault();
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key + " Pre", null, DateTime.Now.ToShortDateString(), DateTime.Now.AddMinutes(100).ToString("HH:mm"), null, DateTime.Now.AddMinutes(160).ToString("HH:mm"), null, null, null, null, null, null, null, null, null, null, null, null);
            res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, DateTime.Now.ToShortDateString(), DateTime.Now.AddMinutes(105).ToString("HH:mm"), null, DateTime.Now.AddMinutes(110).ToString("HH:mm"), null, null, null, null, null, null, null, null, null, null, null, null);
            var items = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session).Value;
            var item = items.Where(x => x.title == key).FirstOrDefault();

            //Assert
            Assert.IsNotNull(item, "Did not set conflict item");
            Assert.AreEqual(item.statusColor, color.title, "Status color not properly set");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key || x.title == (key + " Pre")).ToList());            
        }

        [TestMethod]
        public void PlanItem_ColorStatus_Current()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            string key = DTTestConstants.TestValue + " Color: current";
            var currentStatus = db.Stati.Where(x => x.id == (int)(DtStatus.Current)).FirstOrDefault();
            var color = db.ColorCodes.Where(x => x.id == currentStatus.colorCode).FirstOrDefault();
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, DateTime.Now.ToShortDateString(), DateTime.Now.AddMinutes(60).ToString("HH:mm"), null, DateTime.Now.AddMinutes(210).ToString("HH:mm"), null, null, null, null, null, null, null, null, null, null, null, null);
            var items = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session).Value;
            var item = items.Where(x => x.title == key).FirstOrDefault();

            //Assert
            Assert.IsNotNull(item, "Did not set current item");
            Assert.AreEqual(item.statusColor, color.title, "Status color not properly set");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key).ToList());           
        }

        [TestMethod]
        public void PlanItem_ColorStatus_Future()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            string key = DTTestConstants.TestValue + " Color: future";
            var futureStatus = db.Stati.Where(x => x.id == (int)(DtStatus.Future)).FirstOrDefault();
            var futureColor = db.ColorCodes.Where(x => x.id == futureStatus.colorCode).FirstOrDefault();
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, DateTime.Now.AddDays(2).ToShortDateString(), "13:30", null, "14:00", null, null, null, null, null, null, null, null, null, null, null, null);
            var items = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session).Value;
            var item = items.Where(x => x.title == key).FirstOrDefault();

            //Assert
            Assert.IsNotNull(item, "Did not set future item");
            Assert.AreEqual(item.statusColor, futureColor.title, "Status color not properly set");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key).ToList());            
        }

        [TestMethod]
        public void PlanItem_ColorStatus_OutOfDate()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            string key = DTTestConstants.TestValue + " Color: Out of date";
            var outOfDateStatus = db.Stati.Where(x => x.id == (int)(DtStatus.Out_of_date)).FirstOrDefault();
            var outOfDateColor = db.ColorCodes.Where(x => x.id == outOfDateStatus.colorCode).FirstOrDefault();
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, DateTime.Now.AddDays(-1).ToShortDateString(), "13:30", null, "14:00", null, null, null, null, null, null, null, null, null, null, null, null);
            var items = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session).Value;
            var item = items.Where(x => x.title == key).FirstOrDefault();

            //Assert
            Assert.IsNotNull(item, "Did not set out of date item");
            Assert.AreEqual(item.statusColor, outOfDateColor.title, "Status color not properly set");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key).ToList());            
        }

        [TestMethod]
        public void PlanItem_ColorStatus_Pastdue()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var pastStart = DateTime.Now.AddMinutes(-200).ToString("HH:mm");
            var pastEnd = DateTime.Now.AddMinutes(-140).ToString("HH:mm");
            string key = DTTestConstants.TestValue + " Color: pastdue";
            var colorStatus = db.Stati.Where(x => x.id == (int)(DtStatus.Pastdue)).FirstOrDefault();
            var color = db.ColorCodes.Where(x => x.id == colorStatus.colorCode).FirstOrDefault();
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, DateTime.Now.ToShortDateString(), pastStart, null, pastEnd, null, null, null, null, null, null, null, null, null, null, null, null);
            var items = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session).Value;
            var item = items.Where(x => x.title == key).FirstOrDefault();

            //Assert
            Assert.IsNotNull(item, "Did not set working item");
            if (DateTime.Now.AddMinutes(-200).ToShortDateString() == DateTime.Now.ToShortDateString()) Assert.AreEqual(item.statusColor, color.title, "Status color not properly set");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key).ToList());            
        }

        [TestMethod]
        public void PlanItem_ColorStatus_Subitem()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            string key = DTTestConstants.TestValue + " Color: subitem";
            var colorStatus = db.Stati.Where(x => x.id == (int)(DtStatus.Subitem)).FirstOrDefault();
            var color = db.ColorCodes.Where(x => x.id == colorStatus.colorCode).FirstOrDefault();
            var color2Status = db.Stati.Where(x => x.id == (int)(DtStatus.Current)).FirstOrDefault();
            var color2 = db.ColorCodes.Where(x => x.id == colorStatus.colorCode).FirstOrDefault();
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key + " Pre", null, DateTime.Now.ToShortDateString(), DateTime.Now.AddMinutes(200).ToString("HH:mm"), null, DateTime.Now.AddMinutes(260).ToString("HH:mm"), null, null, null, null, null, null, null, null, null, null, null, null);
            res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, DateTime.Now.ToShortDateString(), DateTime.Now.AddMinutes(205).ToString("HH:mm"), null, null, null, null, null, null, null, null, null, null, null, null, null, null);
            var items = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session).Value;
            var item = items.Where(x => x.title == key).FirstOrDefault();

            //Assert
            Assert.IsNotNull(item, "Did not set subitem item");
            Assert.IsTrue((item.statusColor == color.title), "Status color not properly set. It is " + item.statusColor);

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key || x.title == (key + " Pre")).ToList());            
        }

        [TestMethod]
        public void PlanItem_ColorStatus_Working()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var tenMinsAgo = DateTime.Now.AddMinutes(-10).ToString("HH:mm");
            var fiftyMinsFromNow = DateTime.Now.AddMinutes(50).ToString("HH:mm");
            string key = DTTestConstants.TestValue + " Color: working";
            var colorStatus = db.Stati.Where(x => x.id == (int)(DtStatus.Working)).FirstOrDefault();
            var color = db.ColorCodes.Where(x => x.id == colorStatus.colorCode).FirstOrDefault();
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, DateTime.Now.ToShortDateString(), tenMinsAgo, null, fiftyMinsFromNow, null, null, null, null, null, null, null, null, null, null, null, null);
            var items = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session).Value;
            var item = items.Where(x => x.title == key).FirstOrDefault();

            //Assert
            Assert.IsNotNull(item, "Did not set working item");
            Assert.AreEqual(item.statusColor, color.title, "Status color not properly set");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key).ToList());            
        }

        [TestMethod]
        public void PlanItem_Get()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var totalPlanItems = db.PlanItems.Where(x => x.user == _testUser.id).ToList();
            var totalPlanItemsCurrent = db.PlanItems.Where(x => x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null))).ToList();
            var numberOfNotCompletedPlanItems = db.PlanItems.Where(x => x.user == _testUser.id && x.completed == null && (x.recurrence == null || x.day >= today)).ToList();
            SetControllerQueryString();

            // Act
            var getResults = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session).Value;
            var getWithCompleted = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session, null, true).Value;

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
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var key = DTTestConstants.TestValue + " for Set Test";
            var totalPlanItems = db.PlanItems.Where(x => x.user == _testUser.id).ToList();
            var totalPlanItemsCurrent = db.PlanItems.Where(x => x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null))).ToList();
            var numberOfNotCompletedPlanItems = db.PlanItems.Where(x => x.user == _testUser.id && ((x.completed.HasValue == false || x.completed.Value == false) || x.day >= today)).ToList();
            SetControllerQueryString();

            // Act
            var jsonRes = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
            var returnedList = (List<dtPlanItemModel>)jsonRes.Value;
            var testItem = returnedList.Where(x => x.title == key).FirstOrDefault();
            var jsonResCompleted = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, testItem.title, DTTestConstants.TestValue2, null, null, null, null, null, null, true, null, null, null, true, null, null, testItem.id);
            var returnedListFromCompleted = (List<dtPlanItemModel>)jsonResCompleted.Value;
            var completedTestItem = returnedListFromCompleted.Where(x => x.title == key && x.note == DTTestConstants.TestValue2).FirstOrDefault();

            // Assert
            Assert.AreEqual(returnedList.Count, totalPlanItemsCurrent.Count + 1, "Did not add test plan item correctly.");
            Assert.AreEqual(testItem.title, key, "Test item title incorrect.");
            Assert.AreEqual(completedTestItem.title, key, "Completed test item wrong title.");
            Assert.AreEqual(completedTestItem.note, DTTestConstants.TestValue2, "Note not set.");
            Assert.IsTrue(completedTestItem.completed.Value);

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key).ToList());            
        }

        [TestMethod]
        public void PlanItemSet_FixedStart()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            string key = DTTestConstants.TestValue + " - Fixed Start";
            string projKey = DTTestConstants.TestValue + " - Fixed Start Proj";
            SetControllerQueryString();
            _controller.SetProject(DTTestOrganizer.TestSession.session, projKey, "FSP", (int)DtStatus.Active, 92, null, null, null, null);
            var proj = db.Projects.Where(x => x.title == projKey).FirstOrDefault();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, DateTime.Now.AddDays(1).ToShortDateString(), "13:00", null, "14:00", null, null, null, null, proj.id, null, null, null, null, null, null, null, true);
            var item = db.PlanItems.Where(x => x.title == key).FirstOrDefault();

            //Assert
            Assert.IsNotNull(item);
            Assert.IsTrue(item.fixedStart.HasValue && item.fixedStart.Value);

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key).ToList());            
        }

        [TestMethod]
        public void PlanItem_Delete()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            _numberOfPlanItems = db.PlanItems.Where(x => x.user == _testUser.id).ToList().Count + 1;
            SetControllerQueryString();
            string key = DTTestConstants.TestValue + " Delete Test";

            // Act
            var jsonRes = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
            var itemSetCt = db.PlanItems.Where(x => x.title == key).ToList().Count;
            var setItem = db.PlanItems.Where(x => x.title == key).FirstOrDefault();
            var delRes = _controller.DeletePlanItem(DTTestOrganizer.TestSession.session, setItem.id);
            var delItemCt = db.PlanItems.Where(x => x.title == key).ToList().Count;

            //Assert
            Assert.AreEqual(itemSetCt, delItemCt + 1, "When set, there should be one more item counts than once deleted.");
            Assert.IsTrue((bool)delRes.Value, "Controller should have confirmed delete.");            
        }

        [TestMethod]
        public void PlanItem_Delete_KeepChildren()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            SetControllerQueryString();
            string key = DTTestConstants.TestValue + " - Delete Rec./Keep Children";
            var jsonRes = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            // Should have 30 children + the recurrence.
            int totalItemsBeforeDelete = db.PlanItems.Where(x => x.title == key).ToList().Count;
            var recurrence = db.PlanItems.Where(x => x.title == key && x.recurrence.HasValue).FirstOrDefault();

            //Act
            var delResult = _controller.DeletePlanItem(DTTestOrganizer.TestSession.session, recurrence.id);

            //Assert
            Assert.AreEqual(totalItemsBeforeDelete, 31, "Recurrence and children not set correctly.");
            Assert.IsNull(db.PlanItems.Where(x => x.title == key && x.recurrence.HasValue).FirstOrDefault(), "Recurrence not deleted.");
            Assert.AreEqual(db.PlanItems.Where(x => x.title==key).ToList().Count, 30, "Children not properly handled");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == key).ToList());            
        }

        [TestMethod]
        public void PlanItem_Delete_RecurrenceAndChildren()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            SetControllerQueryString();
            string key = DTTestConstants.TestValue + " - Delete Rec & Children";
            var jsonRes = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, key, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            // Should have 30 children + the recurrence.
            int totalItemsBeforeDelete = db.PlanItems.Where(x => x.title == key).ToList().Count;
            var recurrence = db.PlanItems.Where(x => x.title == key && x.recurrence.HasValue).FirstOrDefault();

            //Act
            var delResult = _controller.DeletePlanItem(DTTestOrganizer.TestSession.session, recurrence.id, true);

            //Assert
            Assert.AreEqual(totalItemsBeforeDelete, 31, "Recurrence and children not set correctly.");
            Assert.IsNull(db.PlanItems.Where(x => x.title == key && x.recurrence.HasValue).FirstOrDefault(), "Recurrence not deleted.");
            Assert.AreEqual(db.PlanItems.Where(x => x.title == key).ToList().Count, 0, "Children not properly handled");            
        }

        [TestMethod]
        public void PlanItem_RecurrenceNotCurrent()
        {
            // If a recurrence is set for a previous date, when the plan items are retrieved, there should be items populated for the next 30 days.

            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = db.PlanItems.Where(x => x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null))).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " for  Out of Date Recurrence Test";
            var testProj = db.Projects.Where(x => x.user == _testUser.id).FirstOrDefault();
            var targDate = DateTime.Now.AddDays(-50);
            DayOfWeek weekdayToday = DateTime.Now.DayOfWeek;
            int numberOfChildrenExpected = weekdayToday >= DayOfWeek.Monday && weekdayToday <= DayOfWeek.Thursday ? 9 : 8;
            SetControllerQueryString();

            //Act
            var jsonSetResult = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey, null, targDate.ToShortDateString(), null, null, null, null, null, null, null, null, null, true, null, null, null, DTTestConstants.RecurrenceId_Daily, "--*-*--");
            var itemList = (List<dtPlanItemModel>)_controller.PlanItems(DTTestOrganizer.TestSession.session, null, true).Value;
            Console.WriteLine(itemList.Count);

            //Assert
            Assert.AreEqual(itemList.Count, numberOfPlanItems + numberOfChildrenExpected, "When getting the plan items, it should have populated up the number of items to include the expected number of children plus 1 for the recurrence.");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey).ToList());            
        }

        [TestMethod]
        public void PlanItemSet_DailyRecurrence_TTh_Filter()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = db.PlanItems.Where(x => x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null))).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " set item through API with TTh Recurrence";
            //Most of the time we expect 30 days ahead to generate 8 T-Th unless we are M, T, W, or Th, then the extra 2 days will add a T-Th
            DayOfWeek weekdayToday = DateTime.Now.DayOfWeek;
            int numberOfChildrenExpected = weekdayToday >= DayOfWeek.Monday && weekdayToday <= DayOfWeek.Thursday ? 9 : 8;
            SetControllerQueryString();

            //Act
            var jsonSetRes = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey, null, null, null, null, null, null, null, null, null, null, null, true, null, null, null, DTTestConstants.RecurrenceId_Daily, "--*-*--");
            var returnedList = (List<dtPlanItemModel>)jsonSetRes.Value;

            //Assert
            Assert.AreEqual(returnedList.Count, numberOfPlanItems + numberOfChildrenExpected + 1, "Setting the recurring plan item should have increased number of plan items by 1 and the expected number of children..");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey).ToList());            
        }

        [TestMethod]
        public void PlanItemSet_Monthly_nth_Monday_past()
        {
            // Setting a recurrence of 3rd M & F in a month => 3:-*---*-. We are setting the start date equal to 14 days previous to today.

            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var nextToEndDay = today.AddDays(28);
            var endDay = today.AddDays(29);
            var start = DateTime.Now.ToString("HH:mm");
            var end = DateTime.Now.AddMinutes(20).ToString("HH:mm");

            var numberOfPlanItems = db.PlanItems.Where(x => x.user == _testUser.id && x.day >= today).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " past recurrence with 3rd Monday & Wednesday of month Recurrence";
            //We expect 30 days ahead to generate 2 M-F items.            
            int expectedChildren = 0;
            for (int i = 0; i < 30; i++)
            {
                var test = today.AddDays(i);
                if ((int)((test.Day + 6) / 7) == 3 && (test.DayOfWeek == DayOfWeek.Monday || test.DayOfWeek == DayOfWeek.Wednesday)) expectedChildren++;
            }
            
            //If the current week contains a 3rd Monday/Wednesday && either of the last two days are Monday or Wednesday, then we need an extra child.
            /*          if ((today.Day >= 21 && today.Day < 28) &&
                          (nextToEndDay.DayOfWeek == DayOfWeek.Monday || 
                           nextToEndDay.DayOfWeek == DayOfWeek.Wednesday || 
                           endDay.DayOfWeek == DayOfWeek.Monday || 
                           endDay.DayOfWeek == DayOfWeek.Wednesday
                          )
                         )
                      {
                          expectedChildren++;
                      }
            */
            
            SetControllerQueryString();

            //Act
            var setRes = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey, null, today.AddDays(-14).ToShortDateString(), start, null, end, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Monthly_nth_day, "3:-*-*---");
            var recurrence = db.PlanItems.Where(x => x.user == _testUser.id && x.title == planItemKey && x.recurrence.HasValue && x.recurrence.Value == (int)DtRecurrence.Monthly_nth_day).FirstOrDefault();
            var children = db.PlanItems.Where(x => x.user == _testUser.id && x.parent.HasValue && x.parent.Value == recurrence.id).ToList();

            //Assert
            Assert.IsNotNull(recurrence, "Recurrence not set.");
            Assert.AreEqual(expectedChildren, children.Count, "Did not propogate correctly.");
            Assert.IsTrue(expectedChildren == 0 || children[0].day.DayOfWeek == DayOfWeek.Monday || children[0].day.DayOfWeek == DayOfWeek.Wednesday, "Day of week incorrect.");


            //Antiseptic
            db.Delete(children);
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey && !x.parent.HasValue).ToList());            
        }

        [TestMethod]
        public void PlanItemSet_MonthlyRecurrence()
        {
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfPlanItems = db.PlanItems.Where(x => x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null))).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " set item through API with 1st & 15th Recurrence";
            string dayOfMonthTargets = "1,15";
            //Generally, we expect to generate 2 children.
            int numberOfChildrenExpected = 2;
            var dateAfter30days = DateTime.Now.AddDays(29);
            bool no15th = false;
            bool no1st = false;
            //May need to adjust number of children.
            if (DateTime.Now.Day == 2 && dateAfter30days.Day == 31)
            {
                numberOfChildrenExpected--;
                no1st = true;
            }
            if (DateTime.Now.Day == 16 && dateAfter30days.Day < 15) 
            {
                numberOfChildrenExpected--;
                no15th = true;
            }
            if ((DateTime.Now.Day == 31 && dateAfter30days.Day == 1) ||
                (DateTime.Now.Day == 14 && dateAfter30days.Day == 15) ||
                (DateTime.Now.Day == 15 && dateAfter30days.Day == 16)) numberOfChildrenExpected++;
            SetControllerQueryString();

            //Act
            var jsonSetRes = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey, null, null, null, null, null, null, null, null, null, null, null, true, null, null, null, (int) DtRecurrence.Monthly, dayOfMonthTargets);
            var returnedList = (List<dtPlanItemModel>)jsonSetRes.Value;
            var setItem = db.PlanItems.Where(x => x.title == planItemKey && x.recurrence == (int)DtRecurrence.Monthly).FirstOrDefault();
            var childItemFor1st = returnedList.Where(x => x.day.Day == 1 && x.parent.HasValue && x.parent.Value == setItem.id).FirstOrDefault();
            var childItemFor15th = returnedList.Where(x => x.day.Day == 15 && x.parent.HasValue && x.parent.Value == setItem.id).FirstOrDefault();

            //Assert
            Assert.IsNotNull(setItem, "Did not set the recurrance.");
            Assert.AreEqual(db.PlanItems.Where(x => x.title == planItemKey).ToList().Count, numberOfChildrenExpected + 1, "Setting the recurring plan item should have increased number of plan items by 1 and the expected number of children.."); ;
            if (!no1st) Assert.IsNotNull(childItemFor1st, "No item set for 1st.");
            if (!no15th) Assert.IsNotNull(childItemFor15th, "No item set for 15th.");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey && !x.parent.HasValue).ToList());            
        }

        [TestMethod]
        public void PlanItem_SemiMonthlyRecurrence()
        {            
            // Setting a recurrence with a 3 week cycle on M & F => 3:-*---*-. We are setting the start date equal to 14 days previous to today.
            // This means that we are beginning the 3rd week, and we expect to see entries on the next Monday and Friday, and then again in 3 weeks.

            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var start = DateTime.Now.ToString("HH:mm");
            var end = DateTime.Now.AddMinutes(20).ToString("HH:mm");

            var numberOfPlanItems = db.PlanItems.Where(x => x.user == _testUser.id && x.day >= today).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " recurrence with 3 weeks MF Recurrence";
            //Most of the time we expect 30 days ahead to generate 3 M-F items. The exception is if today is a Tuesday.
            int expectedChildren = (DateTime.Now.DayOfWeek == DayOfWeek.Sunday || DateTime.Now.DayOfWeek == DayOfWeek.Monday || DateTime.Now.DayOfWeek==DayOfWeek.Thursday || DateTime.Now.DayOfWeek==DayOfWeek.Friday) ? 3 : 2;
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey, null, today.AddDays(-14).ToShortDateString(), start, null, end, null, null, null, null, null, null, null, null, null, null, (int)DtRecurrence.Semi_monthly, "3:-*---*-");
            var recurrence = db.PlanItems.Where(x => x.user == _testUser.id && x.recurrence.HasValue && x.title == planItemKey).FirstOrDefault();
            var children = db.PlanItems.Where(x => x.parent.HasValue && x.parent.Value == recurrence.id).ToList();

            //Assert
            Assert.IsNotNull(res, "Recurrence not saved.");
            Assert.AreEqual(children.Count, expectedChildren, "Unexpected number of children.");

            //Antiseptic
            db.Delete(children);
            db.Delete(recurrence);              
        }

        [TestMethod]
        public void PlanItem_SemiMonthlyRecurrence_Future()
        {            
            // Setting a recurrence with a 3 week cycle on M & F => 3:-*---*-. We are setting the start date equal to 14 days in the future.
            // This means that it is 2 weeks until the beginning the 3rd week, and we expect to see entries on the next Monday and Friday after,
            // but the next cycle is 35 days aways, so there should be only these two.

            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var startTime = DateTime.Now.AddHours(1).ToString("HH:mm");
            var numberOfPlanItems = db.PlanItems.Where(x => x.user == _testUser.id && x.day >= today).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " future recurrence with 3 weeks MF Recurrence";
            //For this test we always expect 2 children to be populated. By setting the start date 14 days ahead, the next M & F including the
            // possible start should be in the 30 day range that is populated, but  the next cycle would be at day 35-41.
            int expectedChildren = 2;

            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey, null, today.AddDays(14).ToShortDateString(), startTime, null, null, null, null, null, null, null, null, null, null, null, null, (int)DtRecurrence.Semi_monthly, "3:-*---*-");
            var recurrence = db.PlanItems.Where(x => x.user == _testUser.id && x.recurrence.HasValue && x.title == planItemKey).FirstOrDefault();
            var children = db.PlanItems.Where(x => x.parent.HasValue && x.parent.Value == recurrence.id).ToList();

            //Assert
            Assert.IsNotNull(res, "Recurrence not saved.");
            Assert.AreEqual(children.Count, expectedChildren, "Unexpected number of children.");
            Assert.IsTrue(children[0].start.Value >= recurrence.start.Value, "Child should not start before the recurrence.");

            //Anticeptic
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey).ToList());            
        }

        [TestMethod]
        public void PlanItem_SetWithDailyRecurrence()
        {            
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            var numberOfNotCompletedPlanItems = db.PlanItems.Where(x => x.user == _testUser.id && (x.day >= today || (x.recurrence == null && x.completed == null))).ToList().Count;
            string planItemKey = DTTestConstants.TestValue + " set with Recurrence";
            SetControllerQueryString();

            //Act
            var jsonSetRes = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey, null, null, null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            var returnedList = (List<dtPlanItemModel>)jsonSetRes.Value;

            //Assert
            Assert.AreEqual(returnedList.Count, numberOfNotCompletedPlanItems + 31, "Setting the recurring plan item should have increased number of plan items by 1 and one for each of the next 30 days.");            
        }

        [TestMethod]
        public void Project_Delete_DeleteItemsToo()
        {            
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            string projectKey = DTTestConstants.TestValue + " project for proj del test";
            string projShortCode = "PDT";
            string recurrenceKey = DTTestConstants.TestValue + " recurrence for proj del test";
            SetControllerQueryString();
            _controller.SetProject(DTTestOrganizer.TestSession.session, projectKey, projShortCode, (int)DtStatus.Active);
            var project = db.Projects.Where(x => x.title == projectKey).FirstOrDefault();
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, recurrenceKey, null, null, null, null, null, null, null, null, null, project.id, null, null, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            var recurrence = db.PlanItems.Where(x => x.project == project.id && x.recurrence.HasValue).FirstOrDefault();
            var projItems = db.PlanItems.Where(x => x.project == project.id && x.parent.HasValue && x.parent.Value == recurrence.id).ToList();

            //Act
            _controller.DeleteProject(DTTestOrganizer.TestSession.session, project.id);

            //Assert
            Assert.IsNotNull(project, "Project not initially created.");
            Assert.IsNotNull(recurrence, "Project recurrence not successfully created.");
            Assert.AreEqual(projItems.Count, 30, "Project recurrence not correctly propagated.");
            Assert.AreEqual(db.PlanItems.Where(x => x.project == project.id && x.parent.HasValue && x.parent.Value == recurrence.id).ToList().Count, 0, "All propagated items not deleted.");
            Assert.IsNull(db.PlanItems.Where(x => x.project == project.id && x.recurrence.HasValue).FirstOrDefault(), "Recurrence not deleted properly.");
            Assert.IsNull(db.Projects.Where(x => x.title == projectKey).FirstOrDefault(), "Project not deleted.") ;            
        }

        [TestMethod]
        public void Project_Delete_KeepItems()
        {            
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            string projectKey = DTTestConstants.TestValue + " project for proj del test (keep items)";
            string projShortCode = "PKT"; // Project Keep Test
            string recurrenceKey = DTTestConstants.TestValue + " recurrence for proj del test (keep items)";
            SetControllerQueryString();
            _controller.SetProject(DTTestOrganizer.TestSession.session, projectKey, projShortCode, (int)DtStatus.Active);
            var project = db.Projects.Where(x => x.title == projectKey).FirstOrDefault();
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, recurrenceKey, null, null, null, null, null, null, null, null, null, project.id, null, null, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            var recurrence = db.PlanItems.Where(x => x.project == project.id && x.recurrence.HasValue).FirstOrDefault();
            var projItems = db.PlanItems.Where(x => x.project == project.id && x.parent.HasValue && x.parent.Value == recurrence.id).ToList();

            //Act
            _controller.DeleteProject(DTTestOrganizer.TestSession.session, project.id, false);

            //Assert
            Assert.IsNotNull(project, "Project not initially created.");
            Assert.IsNotNull(recurrence, "Project recurrence not successfully created.");
            Assert.AreEqual(projItems.Count, 30, "Project recurrence not correctly propagated.");
            Assert.AreEqual(db.PlanItems.Where(x => x.parent.HasValue && x.parent.Value == recurrence.id).ToList().Count, 30, "Propagated items deleted.");
            Assert.IsNotNull(db.PlanItems.Where(x => x.id == recurrence.id && x.recurrence.HasValue).FirstOrDefault(), "Recurrence deleted.");
            Assert.IsNull(db.Projects.Where(x => x.title == projectKey).FirstOrDefault(), "Project not deleted.");
            
            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == recurrenceKey && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => x.title == recurrenceKey).ToList());            
        }

        [TestMethod]
        public void Project_Delete_XferItems()
        {            
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            string projectKey = DTTestConstants.TestValue + " project for proj del test (xfer items)";
            string projectXferKey = DTTestConstants.TestValue + " target project for tranfer";
            string projShortCode = "PXT"; // Project Xfer Test
            string projXferShortCode = "XXt"; //Transfer target project
            string recurrenceKey = DTTestConstants.TestValue + " recurrence for proj del test (xfer items)";
            SetControllerQueryString();
            _controller.SetProject(DTTestOrganizer.TestSession.session, projectKey, projShortCode, (int)DtStatus.Active);
            var project = (from x in dbctx.dtProjects where x.title == projectKey select x).FirstOrDefault();
            _controller.SetProject(DTTestOrganizer.TestSession.session, projectXferKey, projXferShortCode, (int)DtStatus.Active);
            var xferProject = (from x in dbctx.dtProjects where x.title == projectXferKey select x).FirstOrDefault();
            _controller.SetPlanItem(DTTestOrganizer.TestSession.session, recurrenceKey, null, null, null, null, null, null, null, null, null, project.id, null, null, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            var recurrence = (from x in dbctx.dtPlanItems where x.project == project.id && x.recurrence.HasValue select x).FirstOrDefault();
            var projItems = (from x in dbctx.dtPlanItems where x.project == project.id && x.parent.HasValue && x.parent.Value == recurrence.id select x).ToList();

            //Act
            _controller.DeleteProject(DTTestOrganizer.TestSession.session, project.id, false, xferProject.id);

            //Assert
            dbctx.Dispose(); // We want to refresh all the tables for the asserts
            dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            Assert.IsNotNull(project, "Project not initially created.");
            Assert.IsNotNull(xferProject, "Transfer project not initially created.");
            Assert.IsNotNull(recurrence, "Project recurrence not successfully created.");
            Assert.AreEqual(projItems.Count, 30, "Project recurrence not correctly propagated.");
            Assert.AreEqual(db.PlanItems.Where(x => x.project == project.id && x.parent.HasValue && x.parent.Value == recurrence.id).ToList().Count, 0, "Propagated items still attached to project.");
            Assert.AreEqual(db.PlanItems.Where(x => x.project == xferProject.id && x.parent.HasValue && x.parent.Value == recurrence.id).ToList().Count, 30, "Propagated items deleted.");
            Assert.IsNull(db.PlanItems.Where(x => x.project == project.id && x.recurrence.HasValue).FirstOrDefault(), "Recurrence not transferred.");
            Assert.IsNotNull(db.PlanItems.Where(x => x.project == xferProject.id && x.recurrence.HasValue).FirstOrDefault(), "Recurrence deleted.");
            Assert.IsNull(db.Projects.Where(x => x.title == projectKey).FirstOrDefault(), "Project not deleted.");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == recurrenceKey && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => x.title == recurrenceKey).ToList());            
        }

        [TestMethod]
        public void Propagate_FromChild()
        {            
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            string planItemKey = DTTestConstants.TestValue + " for propagate_fromchild";
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey, null, null, null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            var recurrenceItem = (from x in dbctx.dtPlanItems where x.user == _testUser.id && x.title == planItemKey && x.recurrence.HasValue && x.recurrence.Value == (int)DtRecurrence.Daily_Weekly select x).FirstOrDefault();
            string recurrenceItemTitle = recurrenceItem.title;
            string recurrenceItemNote = recurrenceItem.note;
            var children = (from x in dbctx.dtPlanItems where x.parent.HasValue && x.parent.Value == recurrenceItem.id select x).ToList();
            string childrenTitle = children[0].title;
            string childrenNote = children[0].note;
            // Change note, and propagate.
            var changeRes = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey + " changed.", "Note set", null, null, null, null, null, null, null, null, null, null, true, null, null, children[0].id, null, null);
            var propRes = _controller.Propagate(DTTestOrganizer.TestSession.session, children[0].id);
            recurrenceItem = (from x in dbctx.dtPlanItems where x.user == _testUser.id && x.title == (planItemKey + " changed.") && x.recurrence.HasValue && x.recurrence.Value == (int)DtRecurrence.Daily_Weekly select x).FirstOrDefault();
            children = (from x in dbctx.dtPlanItems where x.parent.HasValue && x.parent.Value == recurrenceItem.id select x).ToList();

            //Assert
            dbctx.Entry(recurrenceItem).Reload();
            foreach (var c in children) dbctx.Entry(c).Reload();
            Assert.AreEqual(recurrenceItemTitle, planItemKey, "Recurrence item not correctly set.");
            Assert.AreEqual(children.Count, 30, "Should have been 30 children.");
            Assert.AreEqual(childrenTitle, planItemKey, "Child items not correctly set.");
            Assert.AreEqual(recurrenceItem.title, planItemKey + " changed.", "Recurrence item not updated correctly.");
            Assert.AreEqual(recurrenceItem.note, "Note set", "Recurrence item note not updated correctly.");
            Assert.AreEqual(children[0].title, planItemKey + " changed.", "Child item not updated correctly.");
            Assert.AreEqual(children[0].note, "Note set", "Child item note not updated correctly.");
            Assert.AreEqual(children[29].title, planItemKey + " changed.", "Last child item not updated correctly.");
            Assert.AreEqual(children[29].note, "Note set", "Last child item note not updated correctly.");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey).ToList());            
        }

        [TestMethod]
        public void Propagate_Recurrence()
        {            
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var today = DateTime.Parse(DateTime.Now.ToShortDateString());
            string planItemKey = DTTestConstants.TestValue + " for propagate_recurrence";
            SetControllerQueryString();

            //Act
            var res = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey, null, null, null, null, null, null, null, null, null, null, null, true, null, null, null, (int)DtRecurrence.Daily_Weekly, null);
            var recurrenceItem = (from x in dbctx.dtPlanItems where x.user == _testUser.id && x.title == planItemKey && x.recurrence.HasValue && x.recurrence.Value == (int)DtRecurrence.Daily_Weekly select x).FirstOrDefault();
            string recurrenceItemTitle = recurrenceItem.title;
            string recurrenceItemNote = recurrenceItem.note;
            var children =(from x in dbctx.dtPlanItems where x.parent.HasValue && x.parent.Value == recurrenceItem.id select x).ToList();
            string childrenTitle = children[0].title;
            string childrenNote = children[0].note;
            // Change note, and propagate.
            var changeRes = _controller.SetPlanItem(DTTestOrganizer.TestSession.session, planItemKey + " changed.", "Note set", null, null, null, null, null, null, null, null, null, null, true, null, null, recurrenceItem.id, (int)DtRecurrence.Daily_Weekly, null);
            var propRes = _controller.Propagate(DTTestOrganizer.TestSession.session, recurrenceItem.id);
            var recurrenceItemChanged = (from x in dbctx.dtPlanItems where x.id == recurrenceItem.id select x).FirstOrDefault();
            dbctx.Entry(recurrenceItemChanged).Reload();
            children = recurrenceItem == null ? new List<dtPlanItem>() : (from x in dbctx.dtPlanItems where x.parent.HasValue && x.parent.Value == recurrenceItem.id select x).ToList();
            foreach (var c in children) dbctx.Entry(c).Reload();

            //Assert
            Assert.AreEqual(recurrenceItemTitle, planItemKey, "Recurrence item not correctly set.");
            Assert.AreEqual(children.Count, 30, "Should have been 30 children.");
            Assert.AreEqual(childrenTitle, planItemKey, "Child items not correctly set.");
            Assert.AreEqual(recurrenceItemChanged.title, planItemKey + " changed.", "Recurrence item not updated correctly.");
            Assert.AreEqual(recurrenceItem.note, "Note set", "Recurrence item note not updated correctly.");
            Assert.AreEqual(children[0].title, planItemKey + " changed.", "Child item not updated correctly.");
            Assert.AreEqual(children[0].note, "Note set", "Child item note not updated correctly.");
            Assert.AreEqual(children[29].title, planItemKey + " changed.", "Last child item not updated correctly.");
            Assert.AreEqual(children[29].note, "Note set", "Last child item note not updated correctly.");

            //Antiseptic
            var db = new DTDBDataService(_config, dbctx);
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => x.title == planItemKey).ToList());            
        }

        [TestMethod]
        public void Recurrences()
        {            
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            int numberRecurrences = db.RecurrenceDTOs().Count;
            SetControllerQueryString();

            //Act
            var res = _controller.Recurrences(DTTestOrganizer.TestSession.session);
            var firstRecurrence = ((List<dtRecurrenceModel>)res.Value)[0];
            var fisttRecurrenceInDB = db.RecurrenceDTOs().Where(x => x.id == firstRecurrence.id).FirstOrDefault();

            //Assert
            Assert.AreEqual(((List<dtRecurrenceModel>)res.Value).Count, numberRecurrences, "Recurrence numbers don't match.");
            Assert.IsNotNull(fisttRecurrenceInDB, "Did not retrieve any recurrences.");            
        }

        [TestMethod]
        public void SetProject()
        {            
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            int numProjects = DTTestOrganizer._numberOfProjects;
            var testUser = db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            var testStatus = db.Stati.Where(x => x.title == DTTestConstants.TestStatus).FirstOrDefault();
            string projTitle = DTTestConstants.TestProjectTitlePrefix + "_For_SetProject_Test";
            string projTitleUpdated = projTitle + "_Updated";
            var newProj = new dtProject()
            {
                notes = "new test item from PlannerTests:SetProject",
                shortCode = "TST",
                status = testStatus.id,
                title = projTitle,
                user = testUser.id
            };
            SetControllerQueryString();

            //Act
            var projectsWithNewItem = _controller.SetProject(DTTestOrganizer.TestSession.session, newProj.title, newProj.shortCode, newProj.status, newProj.colorCode ?? 0, newProj.priority, newProj.sortOrder, newProj.notes);
            var projectsWithUpdatedItem = _controller.SetProject(DTTestOrganizer.TestSession.session, projTitleUpdated, newProj.shortCode, newProj.status, newProj.colorCode ?? 0, newProj.priority, newProj.sortOrder, newProj.notes + "_Updated");

            //Assert
            Assert.AreEqual(1, db.Projects.Where(x => x.title == projTitle).ToList().Count, "Should be one new project with the title showing it was created here.");
            Assert.AreEqual(1, db.Projects.Where(x => x.title == projTitleUpdated).ToList().Count, "Should be exactly one projected updated through this.");

            //Antiseptic
            db.Delete(db.Projects.Where(x => x.title == newProj.title).ToList());            
        }

        [TestMethod]
        public void Stati()
        {            
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            int numberStati = db.Stati.Count;
            SetControllerQueryString();

            //Act
            var res = _controller.Stati(DTTestOrganizer.TestSession.session);

            //Assert
            Assert.AreEqual(((List<dtStatusModel>)res.Value).Count, numberStati, "Stati numbers don't match.");            
        }

        [TestMethod]
        public void UpdateRecurrence_PopulatesMissing()
        {            
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var testUser = db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            SetControllerQueryString();
            //Need to have a recurrence that has not populated the childrenn yet. Placing it directly into the database.
            dtProject proj = new dtProject()
            { 
                title = DTTestConstants.TestValue + ": Project for Update Recurrennce - Populate Missing", 
                shortCode = "UTST", 
                user = testUser.id, 
                status = (int)DtStatus.Active, 
                colorCode = 4 
            };
            proj = db.Set(proj);
            // Placing a recurrence that is a M/F every 4 weeks, and setting the start of 3 weeks previous to current day.
            dtPlanItem recurrence = new dtPlanItem()
            {
                title = DTTestConstants.TestValue + ": Recurrence that needs to be populated.",
                project = proj.id,
                user = testUser.id,
                day = DateTime.Parse(DateTime.Now.AddDays(-21).ToShortDateString()),
                start = DateTime.Parse(DateTime.Now.AddDays(-21).ToShortDateString()).AddHours(13),
                duration = TimeSpan.Parse("01:30"),
                recurrence = (int)DtRecurrence.Semi_monthly,
                recurrenceData = "4:-*---*-"
            };
            recurrence = db.Set(recurrence);
            int expectedNumberOfChildren = 2;

            //Act
            int startingChildren = db.PlanItems.Where(x => x.parent.HasValue && x.parent.Value == recurrence.id).ToList().Count;
            var result = (int)(_controller.PopulateRecurrences(DTTestOrganizer.TestSession.session, null, true).Value);


            //Assert
            Assert.AreEqual(db.PlanItems.Where(x => x.parent.HasValue && x.parent.Value == recurrence.id).ToList().Count, expectedNumberOfChildren, "Children in database are incorrect.");
  
            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == recurrence.title && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => x.title == recurrence.title).ToList());
            db.Delete(db.Projects.Where(x => x.id == proj.id).FirstOrDefault());            
        }

        [TestMethod]
        public void UpdateRecurrence_PopulatesMissingUsingMFMask()
        {            
            //Arrange
            var dbctx = new dtdb(_config.GetConnectionString("DG"));  // Each thread needs its own context.
            var db = new DTDBDataService(_config, dbctx);
            var testUser = db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            SetControllerQueryString();
            //Need to have a recurrence that has not populated the childrenn yet. Placing it directly into the database.
            dtProject proj = new dtProject()
            {
                title = DTTestConstants.TestValue + ": Project for Update Recurrennce - Populate Missing MF",
                shortCode = "UTMF",
                user = testUser.id,
                status = (int)DtStatus.Active,
                colorCode = 4
            };
            proj = db.Set(proj);
            // Placing a recurrence that is a M/F every 4 weeks, and setting the start of 3 weeks previous to current day.
            dtPlanItem recurrence = new dtPlanItem()
            {
                title = DTTestConstants.TestValue + ": Recurrence that needs to be populated using M/F.",
                project = proj.id,
                user = testUser.id,
                day = DateTime.Parse(DateTime.Now.AddDays(-21).ToShortDateString()),
                start = DateTime.Parse(DateTime.Now.AddDays(-21).ToShortDateString()).AddHours(13),
                duration = TimeSpan.Parse("01:30"),
                recurrence = (int)DtRecurrence.Semi_monthly,
                recurrenceData = "4:-M---F-"
            };
            recurrence = db.Set(recurrence);
            int expectedNumberOfChildren = 2;

            //Act
            int startingChildren = db.PlanItems.Where(x => x.parent.HasValue && x.parent.Value == recurrence.id).ToList().Count;
            var result = (int)(_controller.PopulateRecurrences(DTTestOrganizer.TestSession.session, null, true).Value);


            //Assert
            Assert.AreEqual(db.PlanItems.Where(x => x.parent.HasValue && x.parent.Value == recurrence.id).ToList().Count, expectedNumberOfChildren, "Children in database are incorrect.");

            //Antiseptic
            db.Delete(db.PlanItems.Where(x => x.title == recurrence.title && x.parent.HasValue).ToList());
            db.Delete(db.PlanItems.Where(x => x.title == recurrence.title).ToList());
            db.Delete(db.Projects.Where(x => x.id == proj.id).FirstOrDefault());            
        }

        /*
        [TestMethod]
        public void BadProp()
        {
            SetControllerQueryString("8a5815c2-7497-42f6-b691-06578c9467f5");
            var user = (from x in db.dtUsers where x.id == 2 select x).FirstOrDefault();
            _controller.VM.User = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtUser, dtUserModel>(); })).Map<dtUserModel>(user);

            var result = _controller.PopulateRecurrences("8a5815c2-7497-42f6-b691-06578c9467f5", 0, true);

        }
        */

    }
}
