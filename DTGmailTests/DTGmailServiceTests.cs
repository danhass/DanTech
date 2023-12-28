using DanTech.Services;
using DanTechGoogleAuthTests;
using Microsoft.Extensions.Configuration;

namespace DTGmailTests
{
    [TestClass]
    public class DTGmailServiceTests
    {
        [TestMethod]
        public void ServiceInstantiate()
        {
            //Arrange

            //Act
            var svc = new DTGmailService();

            //Assert
            Assert.IsNotNull(svc);
        }
        [TestMethod]
        public void SendEmailTest()
        {
            //Arrange
            var svc = new DTGmailService();
            var config = DTTestOrganizer.GetConfiguration()!;
            svc.SetConfig(config);
            var userEmail = config.GetValue<string>("Gmail:Email") ?? "";
            var db = DTTestOrganizer.DB();
            var gmailUser = db.Users.Where(x => x.email == userEmail).FirstOrDefault();
            svc.SetAuthToken(gmailUser.token ?? "");
            svc.SetRefreshToken(gmailUser.refreshToken ?? "");
            svc.SetMailMessage("TryIt", gmailUser.email ?? "", new List<string>() { "hass.dan@gmail.com" }, "Test email from DTGmailService", "", "<b>Test</b> body (html)", new List<string>());

            //Act 
            var sent = svc.Send();
            if (svc.GetAuthToken() != gmailUser.token)
            {
                gmailUser.token = svc.GetAuthToken();
                db.Set(gmailUser);
            }

            //Assert
            Assert.IsTrue(sent);
        }
        [TestMethod]
        public void DeleteEmailTests()
        {
            //Arrange
            var svc = new DTGmailService();
            var config = DTTestOrganizer.GetConfiguration()!;
            svc.SetConfig(config);
            var userEmail = config.GetValue<string>("Gmail:Email");
            var db = DTTestOrganizer.DB();
            var gmailUser = db.Users.Where(x => x.email == userEmail).FirstOrDefault();
            svc.SetAuthToken(gmailUser.token ?? "");
            svc.SetRefreshToken(gmailUser.refreshToken ?? "");

            //Act
            var res = svc.DeleteFromFolder();

            //Assert
            Assert.IsTrue(res >= 0);
        }
    }
}