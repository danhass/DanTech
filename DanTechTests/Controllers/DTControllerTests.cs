using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using System.Net;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DanTech.Controllers;
using Microsoft.Extensions.Configuration;
using DanTechTests.Data;
using DanTech.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;

namespace DanTechTests.Controllers
{
    [TestClass]
    public class DTControllerTests
    {
        [TestMethod]
        public void DTControllerTest_loggedIn()
        {
            //Arrange
            var db = DTDB.getDB();
            var config = DTTestConstants.InitConfiguration();
            var controller = DTTestConstants.InitializeDTController(db, DTTestConstants.LocalHostDomain, true, config);           

            //Assert
            Assert.IsNotNull(controller, "Did not successfully instantiate controller.");
        }
 
    }
}
