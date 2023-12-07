using DanTech.Services;

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
    }
}