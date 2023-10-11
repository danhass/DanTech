using DanTech.Services;
using DanTechGoogleAuthTests;
using System.Threading;

namespace DTGoogleAuthTests
{
    [TestClass]
    public class DTGoogleAuthTests
    {
        [TestMethod]
        public void DTGoogleAuth_SetConfigTest()
        {
            //Arrange
            var svc = new DTGoogleAuthService();

            //Act
            svc.SetConfig(DTTestOrganizer.GetConfiguration()!);

            //Assert
            Assert.IsNotNull(svc);
        }

        [TestMethod]
        public void DTGoogleAuth_AuthServiceTest()
        {
            //Arrange 
            var svc = new DTGoogleAuthService();
            svc.SetConfig(DTTestOrganizer.GetConfiguration()!);

            //Act
            var authServiceURL = svc.AuthService(DTTestConstants.TestReturnEndPoint, DTTestConstants.TestReturnEndPoint, new List<string>() { DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleCalendarScope });

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(authServiceURL));
            Assert.IsTrue(authServiceURL.Length > (DTGoogleAuthService.GoogleUserInfoEmailScope.Length + DTGoogleAuthService.GoogleUserInfoProfileScope.Length + DTGoogleAuthService.GoogleCalendarScope.Length));
            Assert.IsTrue(authServiceURL.Contains(DTGoogleAuthService.GoogleUserInfoProfileScope));
        }

        [TestMethod]
        public void DTGoogleAuth_AuthTokenTest()
        {
            //Arrange
            var svc = DTTestOrganizer.Service();

            //Act

            //Assert
            Assert.IsFalse(string.IsNullOrEmpty(DTTestConstants.TestGoogleAuth));
            Assert.IsFalse(string.IsNullOrEmpty(DTTestConstants.TestGoogleRefresh));
        }

        [TestMethod]
        public void DTGoogleAuth_UserInfoTest()
        {
            //Arrange
            var svc = new DTGoogleAuthService();
            svc.SetConfig(DTTestOrganizer.GetConfiguration()!);

            //Act
            var userInfo = svc.GetUserInfo(DTTestConstants.TestGoogleAuth, DTTestConstants.TestGoogleRefresh);

            //Assert
            Assert.IsNotNull(userInfo);
            Assert.AreEqual(userInfo.Email, DTTestConstants.TestKnownGoodUserEmail);
        }

        [TestMethod]
        public void DTGoogleAuth_RefreshAuthTest()
        {
            //Arrange
            var svc = DTTestOrganizer.Service();  

            //Act
            DTTestConstants.TestGoogleAuth = svc.RefreshAuthToken(DTTestConstants.TestGoogleRefresh, new List<string>() { DTGoogleAuthService.GoogleUserInfoProfileScope, DTGoogleAuthService.GoogleUserInfoEmailScope, DTGoogleAuthService.GoogleCalendarScope });
        
            //Assert
            Assert.IsNotNull(DTTestConstants.TestGoogleAuth);
            Assert.IsFalse(string.IsNullOrEmpty(DTTestConstants.TestGoogleAuth));
        }
    }
}