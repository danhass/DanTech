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
    public class DbTests
    {
        private const string TEST_VALUE = "Test Value";
        private const string TRUE_STRING_VALUE = "1";
        private const string TEST_KEY = "Test data element";

        [TestMethod]
        public void InstantiateDB()
        {
            var db = DTDB.getDB();
            Assert.IsNotNull(db);
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
            var name = "test_" + DateTime.Now.Ticks;

            try
            {
                var specUserCt = (from x in db.dtUsers where x.fName == name && x.lName == name && x.type == 1 select x).ToList().Count;

                //Act
                dtUser u = new dtUser() { fName = name, lName = name, type = 1 };
                db.dtUsers.Add(u);
                db.SaveChanges();
                var userCtAfterInsert = (from x in db.dtUsers where 1 == 1 select x).ToList().Count;
                var specUserCtAfterInsert = (from x in db.dtUsers where x.fName == name && x.lName == name && x.type == 1 select x).ToList().Count;
                u = (from x in db.dtUsers where x.fName == name && x.lName == name && x.type == 1 select x).FirstOrDefault();
                var flagBeforeSuspension = u.suspended;
                u.suspended = 1;
                db.SaveChanges();
                u = (from x in db.dtUsers where x.fName == name && x.lName == name && x.type == 1 select x).FirstOrDefault();
                db.dtUsers.Remove(u);
                db.SaveChanges();
                var userCtAfterRemove = (from x in db.dtUsers where 1 == 1 select x).ToList().Count;
                var specUserCtAfterRemove = (from x in db.dtUsers where x.fName == name && x.lName == name && x.type == 1 select x).ToList().Count;

                //Assert
                Assert.AreEqual(userCt + 1, userCtAfterInsert, "Adding user should increase user count by 1.");
                Assert.AreEqual(userCt, userCtAfterRemove, "Removing user should decrease user count by 1.");
                Assert.AreEqual(specUserCt + 1, specUserCtAfterInsert, "Adding a spec users should increase the count of spec users.");
                Assert.IsNotNull(u, "Inserted user not found.");
                Assert.IsNull(flagBeforeSuspension, "Suspension flag wrongly set.");
                Assert.AreEqual(u.suspended, (byte)1, "Suspension flag should be set.");
                Assert.AreEqual(specUserCt, specUserCtAfterRemove, "Removing spec users should have reduced user count.");
            }
            finally
            {
                var u = (from x in db.dtUsers where x.fName == name && x.lName == name && x.type == 1 select x).FirstOrDefault();
                if (u != null)
                {
                    db.dtUsers.Remove(u);
                    db.SaveChanges();
                }
            }
        }

        [TestMethod]
        public void SetTestingFlag()
        {
            //Arrange
            var db = DTDB.getDB();
            var dataService = new DTDBDataService(db);

            //Act
            dataService.ClearTestData();
            int countOnceClear = db.dtTestData.Count();
            bool setTestElementWhenNotTestingResult = dataService.SetIfTesting(TEST_KEY, TEST_VALUE);
            var testDataFlagAfterNoSet = (from x in db.dtTestData where x.title == TEST_KEY select x).FirstOrDefault();
            dataService.ToggleTestFlag();
            var testInProgressFlag = (from x in db.dtTestData where x.title == dataService.TestFlagKey select x).FirstOrDefault();
            bool setTestDataElementResult = dataService.SetIfTesting(TEST_KEY, TEST_VALUE);
            var testDataElementFlag = (from x in db.dtTestData where x.title == TEST_KEY select x).FirstOrDefault();
            dataService.ClearTestData();
            var testElementsAfterClear = db.dtTestData.ToList().Count;

            //Assert
            Assert.AreEqual(countOnceClear, 0, "Clear test data failed");
            Assert.IsFalse(setTestElementWhenNotTestingResult, "Should not set test value when not testing");
            Assert.IsNull(testDataFlagAfterNoSet, "Should not have set test element when not testing");
            Assert.IsNotNull(testInProgressFlag, "Failed to set test in progress element");
            Assert.AreEqual(testInProgressFlag.value, TRUE_STRING_VALUE, "Test in progress element has wrong value");
            Assert.IsTrue(setTestDataElementResult);
            Assert.IsNotNull(testDataElementFlag);
            Assert.AreEqual(testDataElementFlag.value, TEST_VALUE);
            Assert.AreEqual(testElementsAfterClear, 0, "Was not able to clear the final data elements");            
        }
    }
}
