using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using DanTech.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DanTech.Services;
using System.Threading.Tasks;
using DanTech.Data;
using AutoMapper;
using DanTech.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using K4os.Hash.xxHash;

namespace DanTechTests.Controllers
{
    [TestClass]
    public class NutritionTests
    {
        private IConfiguration _config = DTTestOrganizer.InitConfiguration();
        private NutritionController _controller = null;
        private static dtUser _testUser = null;

        public NutritionTests()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

            var factory = serviceProvider.GetService<ILoggerFactory>();

            var logger = factory.CreateLogger<AdminController>();
            var dbctx = new dtdb(_config.GetConnectionString("DG"));
            var db = new DTDBDataService(_config, dbctx);
            _controller = new NutritionController(_config, logger, db, dbctx);
            _testUser = DTTestOrganizer.TestUser;
            if (_controller != null)
            {
                var testSession = DTTestOrganizer.TestUserSession;
                _controller.VM = new DanTech.Data.Models.DTViewModel();
                _controller.VM.User = new Mapper(new MapperConfiguration(cfg => { cfg.CreateMap<dtUser, dtUserModel>(); })).Map<dtUserModel>(_testUser);
            }
        }

        [TestMethod]
        public void NutritionControllerInstantiate()
        {
            Assert.IsNotNull(_controller, "Nutrition controller not instantiated.");
        }

        [TestMethod]
        public void NutritionControllerIndex()
        {
            //Act
            var result = _controller.Index();

            //Assert
            Assert.AreEqual(result.Value, "DanTech Nutrition Controller");
        }
    }
}
