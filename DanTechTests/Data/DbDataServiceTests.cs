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
        private static dgdb _db = null;
        private static string classTestName = "";
        public static int _numberOfProjects = 0;

        [AssemblyInitialize()]
        public static void Init(TestContext context)
        {
            _db = DTTestConstants.DB(DTTestConstants.DefaultNumberOfTestPropjects);
            _numberOfProjects = DTTestConstants.DefaultNumberOfTestPropjects;
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            DTTestConstants.Cleanup(_db);
        }

        [TestMethod]
        public void InstantiateDB()
        {
            Assert.IsNotNull(_db);
        }
         
        [TestMethod]
        public void DBAccessible()
        {
            var typeCt = (from x in _db.dtTypes where 1 == 1 select x).ToList().Count;
            Assert.IsTrue(typeCt > 0);
        }

        [TestMethod]
        public void DBUserCRUD()
        {
            //Arrange
            var userCt = (from x in _db.dtUsers where 1 == 1 select x).ToList().Count;
            classTestName = "test_" + DateTime.Now.Ticks;

            var specUserCt = (from x in _db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).ToList().Count;

            //Act
            dtUser u = new dtUser() { fName = classTestName, lName = classTestName, type = 1 };
            _db.dtUsers.Add(u);
            _db.SaveChanges();
            var userCtAfterInsert = (from x in _db.dtUsers where 1 == 1 select x).ToList().Count;
            var specUserCtAfterInsert = (from x in _db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).ToList().Count;
            u = (from x in _db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).FirstOrDefault();
            var flagBeforeSuspension = u.suspended;
            u.suspended = 1;
            _db.SaveChanges();
            u = (from x in _db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).FirstOrDefault();
            _db.dtUsers.Remove(u);
            _db.SaveChanges();
            var userCtAfterRemove = (from x in _db.dtUsers where 1 == 1 select x).ToList().Count;
            var specUserCtAfterRemove = (from x in _db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).ToList().Count;

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
        public void SetTestingFlag()
        {
            //Arrange
            var dataService = new DTDBDataService(_db);

            //Act

            //Turn off testing
            bool testFlagBeforeToggle = dataService.InTesting;
            if (testFlagBeforeToggle)
            {
                dataService.ToggleTestFlag();
                testFlagBeforeToggle = dataService.InTesting;
            }

            //Now turn it on.
            dataService.ToggleTestFlag();
            bool testFlagShouldBeSet = dataService.InTesting;
            var testInProgressFlag = (from x in _db.dtTestData where x.title == dataService.TestFlagKey select x).FirstOrDefault();
            bool setTestDataElementResult = DTDBDataService.SetIfTesting(DTTestConstants.TestElementKey, DTTestConstants.TestValue);
            var testDataElementFlag = (from x in _db.dtTestData where x.title == DTTestConstants.TestElementKey select x).FirstOrDefault();

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
            var numStati = (from x in _db.dtStatuses select x).ToList().Count;
            var dataService = new DTDBDataService(_db);

            //Act
            var statuses = dataService.GetStati();

            //Assert
            Assert.AreEqual(statuses.Count, numStati, "Status not correctly retrieved.");
        }

        [TestMethod]
        public void ColorCode_List()
        {
            //Arrange
            var numColorCodes = (from x in _db.dtColorCodes select x).ToList().Count;
            var dataService = new DTDBDataService(_db);

            //Act
            var colorCodes = dataService.GetColorCodes();

            //Assert
            Assert.AreEqual(colorCodes.Count, numColorCodes, "Color codes not correctly received.");
        }

        [TestMethod]
        public void UserModelForSession_NotLoggedIn()
        {
            //Arrange 
            var dataService = new DTDBDataService(_db);
        }

        [TestMethod]
        public void Project_Set()
        {
            //Arrange
            var dataService = new DTDBDataService(_db);
            var testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var testStatus = (from x in _db.dtStatuses where x.title == DTTestConstants.TestStatus select x).FirstOrDefault();
            var allProjects = (from x in _db.dtProjects select x).OrderBy(x => x.id).ToList();
            var existingProject = allProjects[allProjects.Count - 1]; //The last three are test projects
            var copyOfExisting = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtProject, dtProject>(); })).Map<dtProject>(existingProject);
            copyOfExisting.notes = "Updated by Test:Project_Set";
            var newProj = new dtProject()
            {
                notes = "new test item from Test:Project_Set",
                shortCode = "TST",
                status = testStatus.id,
                title = DTTestConstants.TestProjectTitlePrefix + "New_Test", 
                user=testUser.id
            };

            //Act
            var setNew_Result = dataService.Set(newProj);
            var setExist_Result = dataService.Set(copyOfExisting);
            _numberOfProjects++;

            //Assert
            Assert.AreEqual(setNew_Result.id, existingProject.id + 1, "Should have inserted a new project just after the last current one.");
            Assert.AreEqual(setExist_Result.notes, existingProject.notes, "Should have updated existing project to show new note.");
        }

        [TestMethod]
        public void ProjectsListByUser()
        {  
            //Arrange
            var dataService = new DTDBDataService(_db);
            var testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            
            //Act
            var numProjs = dataService.DTProjects(testUser.id);
            var numProjsByUser = dataService.DTProjects(testUser);

            //Assert
            Assert.AreEqual(numProjs.Count, _numberOfProjects, "Data service returns wrong number by user id.");
            Assert.AreEqual(numProjsByUser.Count, _numberOfProjects, "Data service returns wrong number by user.");
        }
        
        [TestMethod]
        public void PlanItemAdd_MinimumItem()
        { 
            //Arrange
            var dataService = new DTDBDataService(_db);
            var testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<dtUser, dtUserModel>());
            var mapper = new Mapper(config);
            dtPlanItemModel model = new dtPlanItemModel()
            {
                title = DTTestConstants.TestPlanItemMinimumTitle,
                day = DateTime.Now.AddDays(2).Date,
                user = mapper.Map<dtUserModel>(testUser),
                userId = testUser.id                
            };

            dtPlanItemModel model2 = new dtPlanItemModel()
            {
                title = DTTestConstants.TestPlanItemAdditionalTitle,
                day = DateTime.Now.AddDays(1).Date,
                user = mapper.Map<dtUserModel>(testUser),
                userId = testUser.id,
                note = DTTestConstants.TestValue2
            };

            //Act
            var item = dataService.Set(model);
            int newItemId = item.id;
            item.note = DTTestConstants.TestValue;
            item = dataService.Set(item);
            var item2 = dataService.Set(model2);
            var itemList = dataService.GetPlanItems(testUser);

            //Assert
            Assert.IsTrue(newItemId > 0, "Plan item creation failed.");
            Assert.IsTrue(item.id == newItemId, "Plan item update did not work.");
            Assert.AreEqual(item.note, DTTestConstants.TestValue, "Did not properly update plan item.");
            Assert.IsTrue(item2.id > item.id, "Order of item creation is not correct.");
            Assert.AreEqual(item2.note, DTTestConstants.TestValue2, "Second test value not set correctly.");
            Assert.AreEqual(itemList.Count, 2, "Did not get list of plan items correctly.");
            Assert.IsTrue(itemList[0].id > itemList[1].id, "Date ordering of plan items is not correct");
        }

        [TestMethod]
        public void PlanItemAdd_NoEndDate()
        {           
            //Arrange
            var dataService = new DTDBDataService(_db);
            var testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
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
                testUser.id,
                mapper.Map<dtUserModel>(testUser),
                null,
                null
                );

            //Act
            var item = dataService.Set(model);
            var ts = item.duration;
            var tsExpected = TimeSpan.Parse("2:05");

            //Assert
            Assert.IsNotNull(item, "Item is null.");
            Assert.AreEqual(ts.Value.Hours, tsExpected.Hours, "Hours are not what is expected.");
            Assert.AreEqual(ts.Value.Minutes, tsExpected.Minutes, "Minutes are not what is expected.");
            Assert.AreEqual(ts.Value.Seconds, tsExpected.Seconds, "Something is wrong with seconds.");
            
        }

    }
}
