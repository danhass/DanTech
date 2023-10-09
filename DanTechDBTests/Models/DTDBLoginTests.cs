using DanTech.Data;
using DanTech.Data.Models;
using DanTech.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DanTechDBTests.Models
{
    [TestClass]
    public class DTDBLoginTests
    {
        [TestMethod]
        public void DTDBLogin_SetData()
        {
            //Arrange
            var svc = DTTestOrganizer.DB() as DTDBDataService;
            var db = svc.db() as dtdb;
            var login = new dtLogin();
            login.FName = DTTestConstants.TestString;
            login.LName = DTTestConstants.TestString2;
            login.Session = DTTestConstants.TestString3;
            login.Message = DTTestConstants.TestString4;

            //Assert
            Assert.IsNotNull(login);
            Assert.IsFalse(string.IsNullOrEmpty(login.FName));
            Assert.IsFalse(string.IsNullOrEmpty(login.LName));
            Assert.IsFalse(string.IsNullOrEmpty(login.Session));
            Assert.IsFalse(string.IsNullOrEmpty(login.Message));
            Assert.AreEqual(login.FName, DTTestConstants.TestString);
            Assert.AreEqual(login.LName, DTTestConstants.TestString2);
            Assert.AreEqual(login.Session, DTTestConstants.TestString3);
            Assert.AreEqual(login.Message, DTTestConstants.TestString4);
        }
    }
}
