using DanTechGoogleAuthTests;
using DTUserManagement.Services;
namespace DTUserManagementTests
{
    [TestClass]
    public class UMServiceTests
    {
        private static string _targetEmail = "hass.dan@gmail.com";
        private static string _baseUrl = @"https://7822-54268.el-alt.com/";

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
            var svc = new DTRegistration(DTTestOrganizer.DB());
            svc.SetConfig(DTTestOrganizer.GetConfiguration());

            //Act
            var regKey = svc.SendRegistration(_targetEmail);
            //var regSent = svc.SendRegistration(_targetEmail, "https://localhost:44324");
        
            //Assert
            var db = DTTestOrganizer.DB();
            var reg = db.Registrations.Where(x => x.email == _targetEmail).FirstOrDefault();
            Assert.IsTrue(regSent.regKey.Length == 6);
            Assert.AreEqual(regSent.email, _targetEmail);
            Assert.AreEqual(reg.email, _targetEmail);
            Assert.AreEqual(regSent.regKey, reg.regKey);

            //Cleanup
            db.Delete(reg);
        }
    }
}