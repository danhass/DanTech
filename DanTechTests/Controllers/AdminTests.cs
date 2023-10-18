using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using DanTech.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DanTech.Services;

namespace DanTechTests.Controllers
{
    [TestClass]
    public class AdminTests
    {
        private static IDTDBDataService _db = null;
        private IConfiguration _config = DTTestOrganizer.InitConfiguration();
        private AdminController _controller = null;

        public AdminTests()
        {
            if (_db == null) _db = DTTestOrganizer.DataService();
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();

            var logger = factory.CreateLogger<AdminController>();

            _controller = new AdminController(_config, logger, _db);
        }

        [TestMethod]
        public void AdminController_InstantiateDefault()
        {
            Assert.IsNotNull(_controller, "Did not instantiate admin controller.");
        }

        [TestMethod]
        public void AdminController_Index()
        {
            //Act
            var res = _controller.Index();

            //Assert
            Assert.IsNotNull(res, "Could not instantiate admin index view");
        }

        /*
        [TestMethod]
        public void AdminController_SetPW()
        {
            //Arrange
            if (_db == null) _db = DTDB.getDB();
            var startPW = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            
            //Act
         }
        */
    }
}
