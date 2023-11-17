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
        [TestMethod]
        public void DTDBLogin_SetLoginTest()
        {
            //Arrange
            var svc = DTTestOrganizer.DB() as DTDBDataService;
            var usr = svc.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();

            //Act
            var login = svc.SetLogin(DTTestConstants.TestUserEmail, DTTestConstants.TestReturnDomain);

            //Assert
            Assert.IsNotNull(login);
            Assert.AreEqual(login.Email, DTTestConstants.TestUserEmail);
            Assert.AreEqual(login.Session, svc.Sessions.Where(x => x.user == usr.id && x.hostAddress == DTTestConstants.TestReturnDomain).FirstOrDefault().session);

            //Cleanup
            svc.Delete(svc.Sessions.Where(x => x.user == usr.id && x.hostAddress == DTTestConstants.TestReturnDomain).ToList());
        }
        [TestMethod]
        public void DTDBLogin_RejectLoginTest()
        {
            //Arrange
            var svc = DTTestOrganizer.DB() as DTDBDataService;

            //Act
            var login = svc.SetLogin(DTTestConstants.TestBadUserEmail, DTTestConstants.TestReturnDomain);

            //Assert
            Assert.IsNull(login);
        }
        [TestMethod]
        public void DTDBLogin_AddUserWithLoginTest()
        {
            //Arrange
            var svc = DTTestOrganizer.DB();
            var testEmail = Guid.NewGuid().ToString() + "@" + Guid.NewGuid().ToString() + ".com";
            var testFName = Guid.NewGuid().ToString();
            var testLName = Guid.NewGuid().ToString();
            var testAuth = Guid.NewGuid().ToString();
            var testRefresh = Guid.NewGuid().ToString();

            //Act
            var login = svc.SetLogin(testEmail, testFName, testLName, DTTestConstants.TestReturnDomain, 1, testAuth, testRefresh);

            //Assert
            Assert.IsNotNull(login);
            Assert.AreEqual(svc.Users.Where(x => x.email == testEmail).FirstOrDefault()!.email, testEmail);
            Assert.AreEqual(login.Email, testEmail);
            Assert.IsFalse(string.IsNullOrEmpty(login.Session));

            //Cleanup
            svc.Delete(svc.Sessions.Where(x => x.session == login.Session).ToList());
            svc.Delete(svc.Users.Where(x => x.email == testEmail).FirstOrDefault()!);
        }
    }
}
