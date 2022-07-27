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

        [AssemblyInitialize()]
        public static void Init(TestContext context)
        {
            _db = DTTestConstants.DB(DTTestConstants.DefaultNumberOfTestPropjects);
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
        public void UserModelForSession_NotLoggedIn()
        {
            //Arrange 
            var dataService = new DTDBDataService(_db);
        }

        [TestMethod]
        public void ProjectsListByUser()
        {  
            //Arrange
            var dataService = new DTDBDataService(_db);

            var testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var numProjs = dataService.DTProjects(testUser.id);
            var numProjsByUser = dataService.DTProjects(testUser);

            //Assert
            Assert.AreEqual(numProjs.Count, DTTestConstants.DefaultNumberOfTestPropjects, "Data service returns wrong number by user id.");
            Assert.AreEqual(numProjsByUser.Count, DTTestConstants.DefaultNumberOfTestPropjects, "Data service returns wrong number by user.");
        }
        
        [TestMethod]
        public void ProjectItemAdd_MinimumItem()
        { 
            //Arrange
            var dataService = new DTDBDataService(_db);
            var testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var config = new MapperConfiguration(cfg => cfg.CreateMap<dtUser, dtUserModel>());
            var mapper = new Mapper(config);
            dtPlanItemModel model = new dtPlanItemModel()
            {
                title = DTTestConstants.TestPlanItemMinimumTitle,
                day = DateTime.Now.AddDays(1).Date,
                user = mapper.Map<dtUserModel>(testUser)
            };

            dtPlanItemModel model2 = new dtPlanItemModel()
            {
                title = DTTestConstants.TestPlanItemAdditionalTitle,
                day = DateTime.Now.AddDays(1).Date,
                user = mapper.Map<dtUserModel>(testUser),
                note = DTTestConstants.TestValue2
            };

            //Act
            var item = dataService.Set(model);
            int newItemId = item.id;
            item.note = DTTestConstants.TestValue;
            item = dataService.Set(item);
            var item2 = dataService.Set(model2);
            var itemList = dataService.Get(testUser);

            //Assert
            Assert.IsTrue(newItemId > 0, "Plan item creation failed.");
            Assert.IsTrue(item.id == newItemId, "Plan item update did not work.");
            Assert.AreEqual(item.note, DTTestConstants.TestValue, "Did not properly update plan item.");
            Assert.IsTrue(item2.id > item.id, "Order of item creation is not correct.");
            Assert.AreEqual(item2.note, DTTestConstants.TestValue2, "Second test value not set correctly.");
            Assert.AreEqual(itemList.Count, 2, "Did not get list of plan items correctly.");
        }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            DTTestConstants.Cleanup(_db);
        }
    }
}
