using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using DanTech.Data;
using DanTechTests.Data;
using DanTech.Models.Data;
using DanTech.Services;
using System;
using AutoMapper;

namespace DanTechTests
{
    [TestClass]
    public class DbDataServiceTests
    {
        private static string classTestName = "";
        private const string _testFlagKey = "Testing in progress";
        private DTDBDataService _service = null;

        public DbDataServiceTests()
        {
            _service = new DTDBDataService(DTDB.getDB(), DTTestOrganizer.Conn);
        }

        [TestMethod]
        public void ColorCode_List()
        {
            //Arrange
            var db = DTDB.getDB();
            var numColorCodes = (from x in db.dtColorCodes select x).ToList().Count;

            //Act
            var colorCodes = _service.ColorCodes();

            //Assert
            Assert.AreEqual(colorCodes.Count, numColorCodes, "Color codes not correctly received.");
        }

        [TestMethod]
        public void DBAccessible()
        {
            var db = DTDB.getDB();
            var typeCt = (from x in db.dtTypes where 1 == 1 select x).ToList().Count;
            Assert.IsTrue(typeCt > 0);
        }

        [TestMethod]
        public void DBUserCRUD()
        {
            //Arrange
            var db = DTDB.getDB();
            var userCt = (from x in db.dtUsers where 1 == 1 select x).ToList().Count;
            classTestName = "test_" + DateTime.Now.Ticks;

            var specUserCt = (from x in db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).ToList().Count;

            //Act
            dtUser u = new dtUser() { fName = classTestName, lName = classTestName, type = 1 };
            db.dtUsers.Add(u);
            db.SaveChanges();
            var userCtAfterInsert = (from x in db.dtUsers where 1 == 1 select x).ToList().Count;
            var specUserCtAfterInsert = (from x in db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).ToList().Count;
            u = (from x in db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).FirstOrDefault();
            var flagBeforeSuspension = u.suspended;
            u.suspended = 1;
            db.SaveChanges();
            u = (from x in db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).FirstOrDefault();
            db.dtUsers.Remove(u);
            db.SaveChanges();
            var userCtAfterRemove = (from x in db.dtUsers where 1 == 1 select x).ToList().Count;
            var specUserCtAfterRemove = (from x in db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).ToList().Count;

            //Assert
            Assert.AreEqual(userCt + 1, userCtAfterInsert, "Adding user should increase user count by 1.");
            Assert.AreEqual(userCt, userCtAfterRemove, "Removing user should decrease user count by 1.");
            Assert.AreEqual(specUserCt + 1, specUserCtAfterInsert, "Adding a spec users should increase the count of spec users.");
            Assert.IsNotNull(u, "Inserted user not found.");
            Assert.IsNull(flagBeforeSuspension, "Suspension flag wrongly set.");
            Assert.AreEqual(u.suspended, (byte)1, "Suspension flag should be set.");
            Assert.AreEqual(specUserCt, specUserCtAfterRemove, "Removing spec users should have reduced user count.");
        }

        [TestMethod]
        public void InstantiateDB()
        {
            var db = DTDB.getDB();
            Assert.IsNotNull(db);
        }

        [TestMethod]
        public void PlanItemAddRecurrence()
        {
            //Arrange
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            string recurrenceTitle = DTTestConstants.TestValue + " for AddRecurrence Test";
            var beginningCount = (from x in db.dtPlanItems where x.user == testUser.id && (x.completed == null || !x.completed.Value) select x).ToList().Count;
            var beginningRecurrenceCt = (from x in db.dtPlanItems where x.user == testUser.id && x.recurrence != null select x).ToList().Count;
            dtPlanItem recurrence = new dtPlanItem() { title = recurrenceTitle, day = DateTime.Parse(DateTime.Now.ToShortDateString()), start = DateTime.Parse(DateTime.Now.ToShortDateString()).AddHours(13), recurrence = DTTestConstants.RecurrenceId_Daily, user = testUser.id };

            //Act
            var results = _service.Set(recurrence);
            var endCount = (from x in db.dtPlanItems where x.user == testUser.id && (x.completed == null || !x.completed.Value) select x).ToList().Count;
            var recurrenceAdded = (from x in db.dtPlanItems where x.id == results.id select x).FirstOrDefault();
            var endRecurrenceCt = (from x in db.dtPlanItems where x.user == testUser.id && x.recurrence != null select x).ToList().Count;
            var childItemCount = (from x in db.dtPlanItems where x.user == testUser.id && x.parent.HasValue && x.parent.Value == recurrenceAdded.id select x).ToList().Count;

            //Assert
            Assert.AreEqual(endCount, beginningCount + 31, "Adding daily recurrence should have added 31 plan items: recurrence + 1 per day for 30 days.");
            Assert.IsNotNull(recurrenceAdded, "Cannot find the recurrence in the database.");
            Assert.AreEqual(endRecurrenceCt, beginningRecurrenceCt + 1, "Adding a recurrence should have increased the recurrence count by 1.");
            Assert.AreEqual(childItemCount, 30, "Should have generated 30 items with the recurrence as a parent.");

            //Antiseptic
            var items = (from x in db.dtPlanItems where x.user == testUser.id && x.parent.Value == recurrenceAdded.id select x).ToList();
            foreach (var item in items) item.parent = null;
            items.Add((from x in db.dtPlanItems where x.id == recurrenceAdded.id select x).FirstOrDefault());
            db.RemoveRange(items);
            db.SaveChanges(); 
        }

        [TestMethod]
        public void PlanItem_ClearPastDue()
        {
            //Arrange
            var db = DTDB.getDB();
            var testUser = DTTestOrganizer.TestUser;
            var numItems = (from x in db.dtPlanItems where x.user == testUser.id select x).Count();
            dtPlanItemModel model = new dtPlanItemModel()
            {
                title = DTTestConstants.TestPlanItemMinimumTitle,
                day = DateTime.Parse(DateTime.Now.ToShortDateString()),
                userId = testUser.id
            };
            dtPlanItemModel modelForPastDue = new dtPlanItemModel()
            {
                title = DTTestConstants.TestPlanItemMinimumTitle + " 2 days old",
                day = DateTime.Parse(DateTime.Now.AddDays(-2).ToShortDateString()),
                completed = true,
                userId = testUser.id
            };
            dtPlanItemModel modelForPastDuePreserve = new dtPlanItemModel()
            {
                title = DTTestConstants.TestPlanItemMinimumTitle + " 2 days old preserved",
                day = DateTime.Parse(DateTime.Now.AddDays(-2).ToShortDateString()),
                completed = true,
                preserve = true,
                userId = testUser.id
            };
            dtPlanItemModel modelForPastDueNotComplete = new dtPlanItemModel()
            {
                title = DTTestConstants.TestPlanItemMinimumTitle + " 2 days old not complete",
                day = DateTime.Parse(DateTime.Now.AddDays(-2).ToShortDateString()),
                userId = testUser.id
            };
            dtPlanItemModel modelForFutureItem = new dtPlanItemModel()
            {
                title = DTTestConstants.TestPlanItemMinimumTitle + " 2 days in future",
                day = DateTime.Parse(DateTime.Now.AddDays(+2).ToShortDateString()),
                userId = testUser.id
            };

            //Act
            var itemsBeforeSet = _service.PlanItems(testUser.id);
            var baseline = _service.Set(model);
            var pastDue = _service.Set(modelForPastDue);
            var preserve = _service.Set(modelForPastDuePreserve);
            var incomplete = _service.Set(modelForPastDueNotComplete);
            var future = _service.Set(model);
            var items = _service.PlanItems(testUser.id);
            var itemsAfterSets = (from x in db.dtPlanItems where x.user == testUser.id select x).Count();

            //Assert
            Assert.AreEqual(itemsAfterSets, numItems + 4, "Should be 4 more items in db after sets with completed past due deleted.");
            Assert.AreEqual(itemsBeforeSet.Count + 3, items.Count, "Only baseline and future should be added to current items.");

            //Antiseptic
            db.dtPlanItems.Remove(baseline);
            //db.dtPlanItems.Remove(pastDue);
            db.dtPlanItems.Remove(preserve);
            db.dtPlanItems.Remove(incomplete);
            db.dtPlanItems.Remove(future);
            db.SaveChanges();
        }

        [TestMethod]
        public void PlanItemSet_MinimumItem()
        {
            //Arrange
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<dtUser, dtUserModel>());
            var mapper = new Mapper(config);
            dtPlanItemModel model = new dtPlanItemModel()
            {
                title = DTTestConstants.TestValue + " Min Item 1",
                day = DateTime.Now.AddDays(2).Date,
                user = mapper.Map<dtUserModel>(testUser),
                userId = testUser.id
            };

            dtPlanItemModel model2 = new dtPlanItemModel()
            {
                title = DTTestConstants.TestValue + " Min Item 2",
                day = DateTime.Now.AddDays(1).Date,
                user = mapper.Map<dtUserModel>(testUser),
                userId = testUser.id,
                note = DTTestConstants.TestValue2
            };

            //Act
            var item = _service.Set(model);
            int newItemId = item.id;
            item.note = DTTestConstants.TestValue;
            item = _service.Set(item);
            var item2 = _service.Set(model2);
            var itemList = _service.PlanItems(testUser);

            //Assert
            Assert.IsTrue(newItemId > 0, "Plan item creation failed.");
            Assert.IsTrue(item.id == newItemId, "Plan item update did not work.");
            Assert.AreEqual(item.note, DTTestConstants.TestValue, "Did not properly update plan item.");
            Assert.AreEqual((from x in db.dtPlanItems where x.id == item.id select x.note).FirstOrDefault(), DTTestConstants.TestValue, "Did not properly update item note.");
            Assert.IsTrue(item2.id > item.id, "Order of item creation is not correct.");
            Assert.AreEqual((from x in db.dtPlanItems where x.id == item2.id select x.note).FirstOrDefault(), DTTestConstants.TestValue2, "Second test value not set correctly.");
            Assert.IsTrue(itemList.Where(x => x.title == model2.title).FirstOrDefault().day < itemList.Where(x => x.title == model.title).FirstOrDefault().day, "Date ordering of plan items is not correct");

            //Antiseptic
            db.Remove(item);
            db.Remove(item2);
            db.SaveChanges();
        }

        [TestMethod]
        public void PlanItemAdd_NoEndDate()
        {
            //Arrange
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<dtUser, dtUserModel>());
            var mapper = new Mapper(config);
            dtPlanItemModel model = new dtPlanItemModel(
                DTTestConstants.TestPlanItemAdditionalTitle,
                string.Empty,
                DateTime.Now.AddDays(5).ToShortDateString(),
                DTTestConstants.TestTimeSpanStart,
                null,
                DTTestConstants.TestTimeSpanEnd,
                null,
                null,
                null,
                true,
                testUser.id,
                mapper.Map<dtUserModel>(testUser),
                null,
                null
                );

            //Act
            var item = _service.Set(model);
            var ts = item.duration;
            var tsExpected = TimeSpan.Parse("2:05");

            //Assert
            Assert.IsNotNull(item, "Item is null.");
            Assert.AreEqual(ts.Value.Hours, tsExpected.Hours, "Hours are not what is expected.");
            Assert.AreEqual(ts.Value.Minutes, tsExpected.Minutes, "Minutes are not what is expected.");
            Assert.AreEqual(ts.Value.Seconds, tsExpected.Seconds, "Something is wrong with seconds.");

            db.Remove(item);
            db.SaveChanges();
        }

        [TestMethod]
        public void PlanItemDelete()
        {
            //Arrange
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<dtUser, dtUserModel>());
            var mapper = new Mapper(config);
            dtPlanItemModel model = new dtPlanItemModel()
            {
                title = DTTestConstants.TestPlanItemMinimumTitle + " Delete Test",
                day = DateTime.Now.AddDays(2).Date,
                user = mapper.Map<dtUserModel>(testUser),
                userId = testUser.id
            };

            //Act
            var item = _service.Set(model);
            int newItemId = item.id;
            int newItemUser = item.user;
            var deleted = _service.DeletePlanItem(newItemId, newItemUser);
            var result = (from x in db.dtPlanItems where x.id == newItemId select x).FirstOrDefault();

            //Assert
            Assert.IsNull(result, "Plan item not properly deleted.");
        }

        [TestMethod]
        public void PlanItemAddRecurrenceWith_TTh_Filter()
        {
            //Arrange
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            string recurrenceTitle = DTTestConstants.TestValue + " for T-Th Recurrence Test";
            var beginningCount = (from x in db.dtPlanItems where x.user == testUser.id && (x.completed == null || !x.completed.Value) select x).ToList().Count;
            var beginningRecurrenceCt = (from x in db.dtPlanItems where x.user == testUser.id && x.recurrence != null select x).ToList().Count;
            //Create a T-Th recurrence
            dtPlanItem recurrence = new dtPlanItem() { title = recurrenceTitle, day = DateTime.Parse(DateTime.Now.ToShortDateString()), start = DateTime.Parse(DateTime.Now.ToShortDateString()).AddHours(14), recurrence = DTTestConstants.RecurrenceId_Daily, recurrenceData = "--*-*--", user = testUser.id };
            //Most of the time we expect 30 days ahead to generate 8 T-Th unless we are M, T, W, or Th, then the extra 2 days will add a T-Th
            DayOfWeek weekdayToday = DateTime.Now.DayOfWeek;
            int numberOfChildrenExpected = weekdayToday >= DayOfWeek.Monday && weekdayToday <= DayOfWeek.Thursday ? 9 : 8;

            //Act
            var results = _service.Set(recurrence);
            var endCount = (from x in db.dtPlanItems where x.user == testUser.id && (x.completed == null || !x.completed.Value) select x).ToList().Count;
            var recurrenceAdded = (from x in db.dtPlanItems where x.id == results.id select x).FirstOrDefault();
            var endRecurrenceCt = (from x in db.dtPlanItems where x.user == testUser.id && x.recurrence != null select x).ToList().Count;
            var childItemCount = (from x in db.dtPlanItems where x.user == testUser.id && x.parent.HasValue && x.parent.Value == recurrenceAdded.id select x).ToList().Count;

            //Assert
            Assert.AreEqual(endCount, beginningCount + numberOfChildrenExpected + 1, "Adding daily recurrence should add plan items: recurrence + number of childrec expeced.");
            Assert.IsNotNull(recurrenceAdded, "Cannot find the recurrence in the database.");
            Assert.AreEqual(endRecurrenceCt, beginningRecurrenceCt + 1, "Adding a recurrence should have increased the recurrence count by 1.");
            Assert.AreEqual(childItemCount, numberOfChildrenExpected, "Should have generated number of children expected with a parent of the recurrence.");

        }

        [TestMethod]
        public void ProjectsListByUser()
        {
            //Arrange
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var numProjects = (from x in db.dtProjects where x.user == testUser.id select x).ToList().Count;

            //Act
            var numProjsFromAPI = _service.DTProjects(testUser.id);
            var numProjsByUser = _service.DTProjects(testUser);

            //Assert
            Assert.AreEqual(numProjsFromAPI.Count, numProjects, "Data service returns wrong number by user id.");
            Assert.AreEqual(numProjsByUser.Count, numProjects, "Data service returns wrong number by user.");
        }

        [TestMethod]
        public void Project_Set()
        {
            //Arrange
            var db = DTDB.getDB();
            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var testStatus = (from x in db.dtStatuses where x.title == DTTestConstants.TestStatus select x).FirstOrDefault();
            var allProjects = (from x in db.dtProjects where x.user == testUser.id select x).OrderBy(x => x.id).ToList();
            var newProj = new dtProject()
            {
                shortCode = "TST",
                status = testStatus.id,
                title = DTTestConstants.TestProjectTitlePrefix + "New_Test #1",
                user = testUser.id
            };
            var newProj2 = new dtProject()
            {
                shortCode = "T2",
                status = testStatus.id,
                title = DTTestConstants.TestProjectTitlePrefix + "New_Test #2",
                user = testUser.id
            };

            //Act
            var setNew_Result = _service.Set(newProj);
            var setNew2 = _service.Set(newProj2);
            setNew2.notes = DTTestConstants.TestValue;
            var setExist_Result = _service.Set(setNew2);
            var allProjectsAfter = (from x in db.dtProjects where x.user == testUser.id select x).OrderBy(x => x.id).ToList();
            var notesUpdated = (from x in db.dtProjects where x.id == setNew_Result.id select x).FirstOrDefault();
            DTTestOrganizer._numberOfProjects++;

            //Assert
            Assert.AreEqual(allProjects.Count + 2, allProjectsAfter.Count, "Should have added 2 new projects.");
            Assert.AreEqual(setNew2.notes, DTTestConstants.TestValue, "Should have updated existing project to show new note.");
        }

        [TestMethod]
        public void Recurrences_List()
        {
            //Arrange
            var db = DTDB.getDB();
            var numRecurrences = (from x in db.dtRecurrences select x).ToList().Count;

            //Act
            var recurrences = _service.Recurrences();

            //Assert
            Assert.AreEqual(recurrences.Count, numRecurrences, "Recurrences not correctly received.");
        }

        [TestMethod]
        public void SetPW_Success()
        {
            //Arrange
            string testPW = DTTestConstants.TestValue;
            _service.SetUser(DTTestOrganizer.TestUser.id);
            var db = DTDB.getDB();
            var startingPW = (from x in db.dtUsers where x.id == DTTestOrganizer.TestUser.id select x.pw).FirstOrDefault();

            //Act
            var res = _service.SetUserPW(testPW);
            var setPW = (from x in db.dtUsers where x.id == DTTestOrganizer.TestUser.id select x.pw).FirstOrDefault();

            //Assert
            Assert.IsTrue(res, "Did not successfully set password.");
            Assert.AreEqual(testPW, setPW, "PW set to wrong value");

            //Cleanup
            var u = (from x in db.dtUsers where x.id == DTTestOrganizer.TestUser.id select x).FirstOrDefault();
            u.pw = startingPW;
            db.SaveChanges();
        }

        [TestMethod]
        public void SetTestingFlag()
        {
            var db = DTDB.getDB();

            //Arrange

            //Act

            //Turn off testing
            bool testFlagBeforeToggle = (from x in db.dtTestData where x.title == _testFlagKey select x).FirstOrDefault() != null;
            if (testFlagBeforeToggle)
            {
                _service.ToggleTestFlag();
                testFlagBeforeToggle = (from x in db.dtTestData where x.title == _testFlagKey select x).FirstOrDefault() != null;
            }

            //Now turn it on.
            _service.ToggleTestFlag();
            bool testFlagShouldBeSet = (from x in db.dtTestData where x.title == _testFlagKey select x).FirstOrDefault() != null;
            var testInProgressFlag = (from x in db.dtTestData where x.title == _testFlagKey select x).FirstOrDefault();
            bool setTestDataElementResult = DTDBDataService.SetIfTesting(DTTestConstants.TestElementKey, DTTestConstants.TestValue);
            var testDataElementFlag = (from x in db.dtTestData where x.title == DTTestConstants.TestElementKey select x).FirstOrDefault();

            //Assert
            Assert.IsFalse(testFlagBeforeToggle, "Did not set initial state to 'not testing'.");
            Assert.AreNotEqual(testFlagBeforeToggle, testFlagShouldBeSet, "Did not toggel test flag correctly.");
            Assert.IsNotNull(testInProgressFlag, "Failed to set test in progress element");
            Assert.IsTrue(testFlagShouldBeSet, "Data service does not reflect db in test state.");
            Assert.AreEqual(testInProgressFlag.value, DTTestConstants.TestStringTrueValue, "Test in progress element has wrong value");
            Assert.IsTrue(setTestDataElementResult, "Failed to set test element.");
            Assert.IsNotNull(testDataElementFlag, "Test data element not correctly set.");
            Assert.AreEqual(testDataElementFlag.value, DTTestConstants.TestValue, "Testing flag not set.");
        }

        [TestMethod]
        public void Statuses_List()
        {
            //Arrange
            var db = DTDB.getDB();
            var numStati = (from x in db.dtStatuses select x).ToList().Count;

            //Act
            var statuses = _service.Stati();

            //Assert
            Assert.AreEqual(statuses.Count, numStati, "Status not correctly retrieved.");
        }

        [TestMethod]
        public void UserModelForSession_NotLoggedIn()
        {
            //Arrange 
        }

    }
}
