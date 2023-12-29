using DanTech.Data;
using DTUserManagement.Services;
namespace DTUserManagementTests
{
    [TestClass]
    public class UMServiceTests
    {
        [TestMethod]
        public void ServiceInstantiate()
        {
            //Arrange

            //Act
            var svc = new DTRegistration();

            //Assert
            Assert.IsNotNull(svc);
        }
        [TestMethod]
        public void RegCodeTests()
        {
            //Arrange
            var svc = new DTRegistration();

            //Act
            var code = svc.RegistrationKey();

            //Assert
            Assert.IsTrue(code.Length == 6, "Code should be six characters long: " + code);
            Assert.IsTrue(int.Parse(code) >= 100000);
            Assert.IsTrue(int.Parse(code) < 1000000);
        }
        [TestMethod]
        public void SendRegMessageTest()
        {
            //Arrange
            var svc = new DTRegistration(DTTestOrganizer.DB()!);
            svc.SetConfig(DTTestOrganizer.GetConfiguration()!);

            //Act
            var result = svc.SendRegistration(DTTestConstants.TestTargetEmail );
        
            //Assert
            var db = DTTestOrganizer.DB();
            var reg = db.Registrations.Where(x => x.email == DTTestConstants.TestTargetEmail).FirstOrDefault();
            Assert.IsTrue(result.regKey.Length == 6);
            Assert.AreEqual(result.email, DTTestConstants.TestTargetEmail);
            Assert.AreEqual(reg.email, DTTestConstants.TestTargetEmail);
            Assert.AreEqual(result.regKey, reg.regKey);

            //Cleanup
            db.Delete(reg);
        }
        [TestMethod]
        public void CompleteRegistrationTest()
        {
            //Arrange
            var db = DTTestOrganizer.DB()!;
            var svc = new DTRegistration(db);
            svc.SetConfig(DTTestOrganizer.GetConfiguration()!);
            var regKey = svc.RegistrationKey();
            dtRegistration reg = new() { email = DTTestConstants.TestFictionalEmail, regKey = regKey, created = DateTime.Now.AddHours(-1) };
            reg = db.Set(reg);

            //Act
            var session = svc.CompleteRegistration(DTTestConstants.TestFictionalEmail, regKey, DTTestConstants.LocatHostIP);

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(session));
            Assert.IsNull(db.Registrations.Where(x => x.email == DTTestConstants.TestFictionalEmail &&  x.regKey == regKey).FirstOrDefault());
            Assert.IsNotNull(db.Sessions.Where(x => x.session == session).FirstOrDefault());
            Assert.IsNotNull(db.Users.Where(x => x.email == DTTestConstants.TestFictionalEmail).FirstOrDefault());
            Assert.IsTrue(db.Users.Where(x => x.email == DTTestConstants.TestFictionalEmail).FirstOrDefault().id == db.Sessions.Where(x => x.session == session).FirstOrDefault().user);

            //Cleanup
            if (db.Sessions.Where(x => x.session == session).FirstOrDefault() != null) db.Delete(db.Sessions.Where(x => x.session == session).FirstOrDefault()!);
            if (db.Users.Where(x => x.email == DTTestConstants.TestFictionalEmail).FirstOrDefault() != null) db.Delete(db.Users.Where(x => x.email == DTTestConstants.TestFictionalEmail).FirstOrDefault()!);
            if (db.Registrations.Where(x => x.email == DTTestConstants.TestFictionalEmail).FirstOrDefault() != null) db.Delete(db.Registrations.Where(x => x.email == DTTestConstants.TestFictionalEmail).FirstOrDefault()!);
        }
    }
}