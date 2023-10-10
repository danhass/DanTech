using DanTech.Data;
using DanTech.Data.Models;
using DanTech.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoMapper;

namespace DanTechDBTests.Models
{
    [TestClass]
    public class DTDBProjectModelTests
    {
        [TestMethod]
        public void DTDBProject_Mapper()
        {
            //Arrange
            var db = DTTestOrganizer.DB() as DTDBDataService;
            var cfg = dtProjectModel.mapperConfiguration;

            //Act
            var mapper = new Mapper(cfg);
            dtProjectModel mdl = mapper.Map<dtProjectModel>(DTTestConstants.TestProject);

            //Assert
            Assert.IsNotNull(mdl);
            Assert.AreEqual(mdl.title, DTTestConstants.TestProject.title);
        }
    }
}
