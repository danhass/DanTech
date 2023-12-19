using AutoMapper;
using DanTech.Data;
using DanTech.Data.Models;
using DanTech.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DanTechDBTests.Service
{
    [TestClass]
    public class DTDBDataServiceTests
    {
        private static string classTestName = "";
        private const string _testFlagKey = "Testing in progress";
        private DTDBDataService? _db = null;

        public DTDBDataServiceTests()
        {
            _db = DTTestOrganizer.DB() as DTDBDataService;
        }

        [TestMethod]
        public void ColorCode_List()
        {
            //Arrange
            var numColorCodes = _db.ColorCodes.Count;

            //Act
            var colorCodes = _db.ColorCodeDTOs();

            //Assert
            Assert.AreEqual(colorCodes.Count, numColorCodes, "Color codes not correctly received.");
        }

        [TestMethod]
        public void DBAccessible()
        {
            var statusCt = _db.Stati.Count;
            Assert.IsTrue(statusCt > 0);
        }

        [TestMethod]
        public void DBUserCRUD()
        {
            //Arrange
            var userCt = _db.Users.Count;
            classTestName = "test_" + DateTime.Now.Ticks;

            var specUserCt = _db.Users.Where(x => x.fName == classTestName && x.lName == classTestName && x.type == 1).ToList().Count;

            //Act
            dtUser? u = new dtUser() { fName = classTestName, lName = classTestName, type = 1 };
            u = _db.Set(u);
            var userCtAfterInsert = _db.Users.Count;
            var specUserCtAfterInsert = _db.Users.Where(x => x.fName == classTestName && x.lName == classTestName && x.type == 1).ToList().Count;
            u = _db.Users.Where(x => x.fName == classTestName && x.lName == classTestName && x.type == 1).FirstOrDefault();
            var flagBeforeSuspension = u.suspended;
            u.suspended = true;
            _db.Save();
            u = _db.Users.Where(x => x.fName == classTestName && x.lName == classTestName && x.type == 1).FirstOrDefault();
            if (u != null) _db.Delete(u!);
            var userCtAfterRemove = _db.Users.Count;
            var specUserCtAfterRemove = _db.Users.Where(x => x.fName == classTestName && x.lName == classTestName && x.type == 1).ToList().Count;

            //Assert
            Assert.AreEqual(userCt + 1, userCtAfterInsert, "Adding user should increase user count by 1.");
            Assert.AreEqual(userCt, userCtAfterRemove, "Removing user should decrease user count by 1.");
            Assert.AreEqual(specUserCt + 1, specUserCtAfterInsert, "Adding a spec users should increase the count of spec users.");
            Assert.IsNotNull(u, "Inserted user not found.");
            Assert.IsNull(flagBeforeSuspension, "Suspension flag wrongly set.");
            Assert.AreEqual(u.suspended, true, "Suspension flag should be set.");
            Assert.AreEqual(specUserCt, specUserCtAfterRemove, "Removing spec users should have reduced user count.");
        }

        [TestMethod]
        public void InstantiateDB()
        {
            Assert.IsNotNull(_db);
        }

        [TestMethod]
        public void RegistrationCRUDTests()
        {
            //Arrange
            var regCt = _db.Registrations.Count;
            dtRegistration testReg = new dtRegistration();
            testReg.email = "test@test.com";
            testReg.regKey = "123456";

            //Act
            var setReg = _db.Set(testReg);
            var regCtAfterSet = _db.Registrations.Count;
            _db.Delete(setReg);
            var regCtAfterDel = _db.Registrations.Count;

            //Assert
            Assert.AreEqual(regCt + 1, regCtAfterSet);
            Assert.AreEqual(setReg.email, testReg.email);
            Assert.AreEqual(setReg.email, "test@test.com");
            Assert.AreEqual(setReg.regKey, testReg.regKey);
            Assert.AreEqual(regCt, regCtAfterDel);
        }

        [TestMethod]
        public void PlanItemAddRecurrence()
        {
            //Arrange
            var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            string recurrenceTitle = DTTestConstants.TestString + " for AddRecurrence Test";
            var beginningCount = _db.PlanItems.Where(x => x.user == testUser.id && (x.completed == null || !x.completed.Value)).ToList().Count;
            var beginningRecurrenceCt = _db.PlanItems.Where(x => x.user == testUser.id && x.recurrence != null).ToList().Count;
            dtPlanItem recurrence = new dtPlanItem() { title = recurrenceTitle, day = DateTime.Parse(DateTime.Now.ToShortDateString()), start = DateTime.Parse(DateTime.Now.ToShortDateString()).AddHours(13), recurrence = (int)DtRecurrence.Daily_Weekly, user = testUser.id };

            //Act
            var results = _db.Set(recurrence);
            var endCount = _db.PlanItems.Where(x => x.user == testUser.id && (x.completed == null || !x.completed.Value)).ToList().Count;
            var recurrenceAdded = _db.PlanItems.Where(x => x.id == results.id).FirstOrDefault();
            var endRecurrenceCt = _db.PlanItems.Where(x => x.user == testUser.id && x.recurrence != null).ToList().Count;
            var childItemCount = _db.PlanItems.Where(x => x.user == testUser.id && x.parent.HasValue && x.parent.Value == recurrenceAdded.id).ToList().Count;

            //Assert
            Assert.AreEqual(endCount, beginningCount + 31, "Adding daily recurrence should have added 31 plan items: recurrence + 1 per day for 30 days.");
            Assert.IsNotNull(recurrenceAdded, "Cannot find the recurrence in the database.");
            Assert.AreEqual(endRecurrenceCt, beginningRecurrenceCt + 1, "Adding a recurrence should have increased the recurrence count by 1.");
            Assert.AreEqual(childItemCount, 30, "Should have generated 30 items with the recurrence as a parent.");

            //Antiseptic
            _db.Delete(_db.PlanItems.Where(x => x.user == testUser.id && x.parent.HasValue && x.parent.Value == recurrenceAdded.id).ToList());
            _db.Delete(results);
        }

        [TestMethod]
        public void PlanItem_ClearPastDue()
        {
            //Arrange
            var testUser = DTTestConstants.TestUser;
            var numItems = _db.PlanItems.Where(x => x.user == testUser.id).Count();
            dtPlanItemModel model = new dtPlanItemModel()
            {
                title = DTTestConstants.TestString + " (Min)",
                day = DateTime.Parse(DateTime.Now.ToShortDateString()),
                userId = testUser.id
            };
            dtPlanItemModel modelForPastDue = new dtPlanItemModel()
            {
                title = DTTestConstants.TestString + " (2 days old)",
                day = DateTime.Parse(DateTime.Now.AddDays(-2).ToShortDateString()),
                completed = true,
                userId = testUser.id
            };
            dtPlanItemModel modelForPastDuePreserve = new dtPlanItemModel()
            {
                title = DTTestConstants.TestString + " (2 days old preserved)",
                day = DateTime.Parse(DateTime.Now.AddDays(-2).ToShortDateString()),
                completed = true,
                preserve = true,
                userId = testUser.id
            };
            dtPlanItemModel modelForPastDueNotComplete = new dtPlanItemModel()
            {
                title = DTTestConstants.TestString + " (2 days old not complete)",
                day = DateTime.Parse(DateTime.Now.AddDays(-2).ToShortDateString()),
                userId = testUser.id
            };
            dtPlanItemModel modelForFutureItem = new dtPlanItemModel()
            {
                title = DTTestConstants.TestString + " (2 days in future)",
                day = DateTime.Parse(DateTime.Now.AddDays(+2).ToShortDateString()),
                userId = testUser.id
            };

            //Act
            var itemsBeforeSet = _db.PlanItemDTOs(testUser.id);
            var baseline = _db.Set(model);
            var pastDue = _db.Set(modelForPastDue);
            var preserve = _db.Set(modelForPastDuePreserve);
            var incomplete = _db.Set(modelForPastDueNotComplete);
            var future = _db.Set(model);
            var items = _db.PlanItemDTOs(testUser.id);
            var itemsAfterSets = _db.PlanItems.Where(x => x.user == testUser.id).ToList();

            //Assert
            Assert.AreEqual(itemsAfterSets.Count, numItems + 4, "Should be 4 more items in db after sets with completed past due deleted.");
            Assert.AreEqual(itemsBeforeSet.Count + 3, items.Count, "Only baseline and future should be added to current items.");

            //Antiseptic
            _db.Delete(baseline);
            _db.Delete(preserve);
            _db.Delete(incomplete);
            _db.Delete(future);
        }

        [TestMethod]
        public void PlanItemSet_MinimumItem()
        {
            //Arrange
            var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<dtUser, dtUserModel>());
            var mapper = new Mapper(config);
            dtPlanItemModel model = new dtPlanItemModel()
            {
                title = DTTestConstants.TestString + " (Min Item 1)",
                day = DateTime.Now.AddDays(2).Date,
                user = mapper.Map<dtUserModel>(testUser),
                userId = testUser.id
            };

            dtPlanItemModel model2 = new dtPlanItemModel()
            {
                title = DTTestConstants.TestString + " (Min Item 2)",
                day = DateTime.Now.AddDays(1).Date,
                user = mapper.Map<dtUserModel>(testUser),
                userId = testUser.id,
                note = DTTestConstants.TestString2
            };

            //Act
            var item = _db.Set(model);
            int newItemId = item.id;
            item.note = DTTestConstants.TestString3;
            item = _db.Set(item);
            var item2 = _db.Set(model2);
            var itemList = _db.PlanItemDTOs(testUser);

            //Assert
            Assert.IsTrue(newItemId > 0, "Plan item creation failed.");
            Assert.IsTrue(item.id == newItemId, "Plan item update did not work.");
            Assert.AreEqual(item.note, DTTestConstants.TestString3, "Did not properly update plan item.");
            Assert.AreEqual(_db.PlanItems.Where(x => x.id == item.id).First().note, DTTestConstants.TestString3, "Did not properly update item note.");
            Assert.IsTrue(item2.id > item.id, "Order of item creation is not correct.");
            Assert.AreEqual(_db.PlanItems.Where(x => x.id == item2.id).First().note, DTTestConstants.TestString2, "Second test value not set correctly.");
            Assert.IsTrue(itemList.Where(x => x.title == model2.title).FirstOrDefault().day < itemList.Where(x => x.title == model.title).FirstOrDefault().day, "Date ordering of plan items is not correct");

            //Antiseptic
            _db.Delete(item);
            _db.Delete(item2);
        }

        [TestMethod]
        public void PlanItemAdd_NoEndDate()
        {
            //Arrange
            var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<dtUser, dtUserModel>());
            var mapper = new Mapper(config);
            dtPlanItemModel model = new dtPlanItemModel(
                DTTestConstants.TestString + " (No end Data)",
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
            var item = _db.Set(model);
            var ts = item.duration!;
            var tsExpected = TimeSpan.Parse("2:05");

            //Assert
            Assert.IsNotNull(item, "Item is null.");
            Assert.AreEqual(ts.Value.Hours, tsExpected.Hours, "Hours are not what is expected.");
            Assert.AreEqual(ts.Value.Minutes, tsExpected.Minutes, "Minutes are not what is expected.");
            Assert.AreEqual(ts.Value.Seconds, tsExpected.Seconds, "Something is wrong with seconds.");

            _db.Delete(item);
        }

        [TestMethod]
        public void PlanItemDelete()
        {
            //Arrange
            var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<dtUser, dtUserModel>());
            var mapper = new Mapper(config);
            dtPlanItemModel model = new dtPlanItemModel()
            {
                title = DTTestConstants.TestString + " (Delete Test)",
                day = DateTime.Now.AddDays(2).Date,
                user = mapper.Map<dtUserModel>(testUser),
                userId = testUser.id
            };

            //Act
            var item = _db.Set(model);
            int newItemId = item.id;
            int newItemUser = item.user;
            var deleted = _db.DeletePlanItem(newItemId, newItemUser);
            var result = _db.Users.Where(x => x.id == newItemId).FirstOrDefault();

            //Assert
            Assert.IsNull(result, "Plan item not properly deleted.");
        }

        [TestMethod]
        public void PlanItemAddRecurrenceWith_TTh_Filter()
        {
            //Arrange
            var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            string recurrenceTitle = DTTestConstants.TestString + " (T-Th Recurrence Test)";
            var beginningCount = _db.PlanItems.Where(x => x.user == testUser.id && (x.completed == null || !x.completed.Value)).ToList().Count;
            var beginningRecurrenceCt = _db.PlanItems.Where(x => x.user == testUser.id && x.recurrence != null).ToList().Count;
            //Create a T-Th recurrence
            dtPlanItem recurrence = new dtPlanItem() { title = recurrenceTitle, day = DateTime.Parse(DateTime.Now.ToShortDateString()), start = DateTime.Parse(DateTime.Now.ToShortDateString()).AddHours(14), recurrence = (int)DtRecurrence.Daily_Weekly, recurrenceData = "--*-*--", user = testUser.id };
            //Most of the time we expect 30 days ahead to generate 8 T-Th unless we are M, T, W, or Th, then the extra 2 days will add a T-Th
            DayOfWeek weekdayToday = DateTime.Now.DayOfWeek;
            int numberOfChildrenExpected = weekdayToday >= DayOfWeek.Monday && weekdayToday <= DayOfWeek.Thursday ? 9 : 8;

            //Act
            var results = _db.Set(recurrence);
            var endCount = _db.PlanItems.Where(x => x.user == testUser.id && (x.completed == null || !x.completed.Value)).ToList().Count;
            var recurrenceAdded = _db.PlanItems.Where(x => x.id == results.id).FirstOrDefault();
            var endRecurrenceCt = _db.PlanItems.Where(x => x.user == testUser.id && x.recurrence != null).ToList().Count;
            var childItemCount = _db.PlanItems.Where(x => x.user == testUser.id && x.parent.HasValue && x.parent.Value == recurrenceAdded.id).ToList().Count;

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
            var db = _db.dtdb();
            var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            var numProjects = (from x in db.dtProjects where x.user == testUser.id select x).ToList().Count;

            //Act
            var numProjsFromAPI = _db.ProjectDTOs(testUser.id);
            var numProjsByUser = _db.ProjectDTOs(testUser);

            //Assert
            Assert.AreEqual(numProjsFromAPI.Count, numProjects, "Data service returns wrong number by user id.");
            Assert.AreEqual(numProjsByUser.Count, numProjects, "Data service returns wrong number by user.");
        }

        [TestMethod]
        public void Project_Set()
        {
            //Arrange
            var testUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
            var testStatus = _db.Stati.Where(x => x.title == DTTestConstants.TestStatus).FirstOrDefault();
            var allProjects = _db.Projects.Where(x => x.user == testUser.id).OrderBy(x => x.id).ToList();
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
            var setNew_Result = _db.Set(newProj);
            var setNew2 = _db.Set(newProj2);
            setNew2.notes = DTTestConstants.TestString + " (Proj note)";
            var setExist_Result = _db.Set(setNew2);
            var allProjectsAfter = _db.Projects.Where(x => x.user == testUser.id).OrderBy(x => x.id).ToList();
            var notesUpdated = _db.Projects.Where(x => x.id == setNew_Result.id).FirstOrDefault();

            //Assert
            Assert.AreEqual(allProjects.Count + 2, allProjectsAfter.Count, "Should have added 2 new projects.");
            Assert.AreEqual(setNew2.notes, DTTestConstants.TestString + " (Proj note)", "Should have updated existing project to show new note.");
            Assert.AreEqual(setExist_Result.notes, DTTestConstants.TestString + " (Proj note)", "Should have updated existing project to show new note.");
 
            //Cleanup
            _db.Delete(setExist_Result);
            _db.Delete(setNew_Result);
        }

        [TestMethod]
        public void Recurrences_List()
        {
            //Arrange
            var numRecurrences = _db.RecurrenceDTOs().ToList().Count;

            //Act
            var recurrences = _db.RecurrenceDTOs();

            //Assert
            Assert.AreEqual(recurrences.Count, numRecurrences, "Recurrences not correctly received.");
        }

        [TestMethod]
        public void SetPW_Success()
        {
            //Arrange
            string testPW = DTTestConstants.TestString;
            _db.SetUser(DTTestConstants.TestUser.id);

            //Act
            var res = _db.SetUserPW(testPW);
            var u = _db.Users.Where(x => x.id == DTTestConstants.TestUser.id).FirstOrDefault();
            var setPW = u.pw;

            //Assert
            Assert.IsTrue(res, "Did not successfully set password.");
            Assert.AreEqual(testPW, setPW, "PW set to wrong value");

            //Cleanup
        }

        [TestMethod]
        public void SetTestingFlag()
        {
            //Arrange

            //Act

            //Turn off testing
            bool testFlagBeforeToggle = _db.TestData.Where(x => x.title == _testFlagKey).FirstOrDefault() != null;
            if (testFlagBeforeToggle)
            {
                _db.ToggleTestFlag();
                testFlagBeforeToggle = _db.TestData.Where(x => x.title == _testFlagKey).FirstOrDefault() != null;
            }

            //Now turn it on.
            _db.ToggleTestFlag();
            bool testFlagShouldBeSet = _db.TestData.Where(x => x.title == _testFlagKey).FirstOrDefault() != null;
            var testInProgressFlag = _db.TestData.Where(x => x.title == _testFlagKey).FirstOrDefault();
            bool setTestDataElementResult = _db.SetIfTesting(DTTestConstants.TestElementKey, DTTestConstants.TestString);
            var testDataElementFlag = _db.TestData.Where(x => x.title == DTTestConstants.TestElementKey).FirstOrDefault();

            //Assert
            Assert.IsFalse(testFlagBeforeToggle, "Did not set initial state to 'not testing'.");
            Assert.AreNotEqual(testFlagBeforeToggle, testFlagShouldBeSet, "Did not toggel test flag correctly.");
            Assert.IsNotNull(testInProgressFlag, "Failed to set test in progress element");
            Assert.IsTrue(testFlagShouldBeSet, "Data service does not reflect db in test state.");
            Assert.AreEqual(testInProgressFlag.value, DTTestConstants.TestStringTrueValue, "Test in progress element has wrong value");
            Assert.IsTrue(setTestDataElementResult, "Failed to set test element.");
            Assert.IsNotNull(testDataElementFlag, "Test data element not correctly set.");
            Assert.AreEqual(testDataElementFlag.value, DTTestConstants.TestString, "Testing flag not set.");
        }

        [TestMethod]
        public void Statuses_List()
        {
            //Arrange
            dtdb db = _db.dtdb();
            var numStati = (from x in db.dtStatuses select x).ToList().Count;

            //Act
            var statuses = _db.StatusDTOs();

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
