using DTUserManagement.Services;
namespace DTUserManagementTests
{
    [TestClass]
    public class UnitTest1
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
    }
}