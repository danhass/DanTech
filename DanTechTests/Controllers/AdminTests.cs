using DanTech.Controllers;
using DanTech.Data;
using DanTechTests.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace DanTechTests.Controllers
{
    [TestClass]
    public class AdminTests
    {
        private static dtdb _db = null;
        private IConfiguration _config = DTTestOrganizer.InitConfiguration();
        private AdminController _controller = null;

        public AdminTests()
        {
            _db = DTDB.getDB();
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
    }
}
