using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using DanTech.Data;
using DanTechTests.Data;
using DanTech.Services;
using System;

namespace DanTechTests
{
    [TestClass]
    public class DbDataServiceTests
    {
        private dgdb _db = DTDB.getDB(3);
        private string classTestName = "";

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
            dataService.ClearTestData();
            int countOnceClear = _db.dtTestData.Count();
            bool setTestElementWhenNotTestingResult = dataService.SetIfTesting(DTTestConstants.TestElementKey, DTTestConstants.TestValue);
            var testDataFlagAfterNoSet = (from x in _db.dtTestData where x.title == DTTestConstants.TestElementKey select x).FirstOrDefault();
            dataService.ToggleTestFlag();
            bool testFlagShouldBeSet = dataService.InTesting;
            var testInProgressFlag = (from x in _db.dtTestData where x.title == dataService.TestFlagKey select x).FirstOrDefault();
            bool setTestDataElementResult = dataService.SetIfTesting(DTTestConstants.TestElementKey, DTTestConstants.TestValue);
            var testDataElementFlag = (from x in _db.dtTestData where x.title == DTTestConstants.TestElementKey select x).FirstOrDefault();
            dataService.ClearTestData();
            var testFlagShouldNotBeSet = dataService.InTesting;
            var testElementsAfterClear = _db.dtTestData.ToList().Count;

            //Assert
            Assert.AreEqual(countOnceClear, 0, "Clear test data failed");
            Assert.IsFalse(setTestElementWhenNotTestingResult, "Should not set test value when not testing");
            Assert.IsNull(testDataFlagAfterNoSet, "Should not have set test element when not testing");
            Assert.IsNotNull(testInProgressFlag, "Failed to set test in progress element");
            Assert.IsTrue(testFlagShouldBeSet, "Data service does not reflect db in test state.");
            Assert.AreEqual(testInProgressFlag.value, DTTestConstants.TestStringTrueValue, "Test in progress element has wrong value");
            Assert.IsTrue(setTestDataElementResult, "Failed to set test element.");
            Assert.IsNotNull(testDataElementFlag, "Test data element not correctly set.");
            Assert.AreEqual(testDataElementFlag.value, DTTestConstants.TestValue);
            Assert.AreEqual(testElementsAfterClear, 0, "Was not able to clear the final data elements");
            Assert.IsFalse(testFlagShouldNotBeSet, "Data service still reflects db in test state.");
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
            var numProjs = (from x in _db.dtProjects where x.title.StartsWith(DTTestConstants.TestProjectTitlePrefix) select x).Count();
            Assert.AreEqual(numProjs, 3, "Number of test projects improperly set.");
        }

        [ClassCleanup]
        public void ClassCleanup()
        {
            var u = (from x in _db.dtUsers where x.fName == classTestName && x.lName == classTestName && x.type == 1 select x).FirstOrDefault();
            if (u != null)
            {
                _db.dtUsers.Remove(u);
                _db.SaveChanges();
            }
        }
    }
}
