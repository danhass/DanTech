using DanTech.Data;
using DanTechTests.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Linq;
using DanTech.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;

namespace DanTechTests.Controllers
{
    [TestClass]
    public class PlannerTests
    {
        private static dgdb _db = null;
        private IConfiguration _config = DTTestConstants.InitConfiguration();
        private PlannerController _controller = null;

        public PlannerTests()
        {
            _db = DTTestConstants.DB();
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();

            var logger = factory.CreateLogger<PlannerController>();
            _controller = new PlannerController(_config, logger, _db);
            var testUser = (from x in _db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            var testSession = (from x in _db.dtSessions where x.user == testUser.id select x).FirstOrDefault();
            if (testSession == null)
            {
                testSession = new dtSession() { user = testUser.id, hostAddress = IPAddress.Loopback.ToString() };
                _db.dtSessions.Add(testSession);
            }
            testSession.expires = DateTime.Now.AddDays(1);
            testSession.session = DTTestConstants.TestSessionId;
            _db.SaveChanges();
        }

        [TestMethod]
        public void ControllerInitialized()
        {
            Assert.IsNotNull(_controller, "Planner controller not correctly initialized.");
        }
    }
}
