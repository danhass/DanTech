using DanTech.Data;
using DanTech.Data.Models;
using DanTech.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace DanTechDBTests.Models
{
    [TestClass]
    public class DTDBPlanItemModelTests
    {
        [TestMethod]
        public void DTDBPlanItemModel_InstFromDBObj() 
        {
            //Arrange
            var db = DTTestOrganizer.DB() as DTDBDataService;

            //Act
            var mdl = new dtPlanItemModel(DTTestConstants.TestPlanItem!);

            //Assert
            Assert.IsNotNull(mdl);
            Assert.AreEqual(mdl.title, DTTestConstants.TestPlanItem.title);
            Assert.AreEqual(mdl.id, DTTestConstants.TestPlanItem.id);
        }

        public void DTDBPlanItemModel_InstDefault()
        {
            //Arrange
            var db = DTTestOrganizer.DB() as DTDBDataService;

            //Act
            var mdl = new dtPlanItemModel();

            //Assert
            Assert.IsNotNull (mdl);
        }

        public void DTDBPlanItemModel_InstWithMinInfo()
        {
            //Arrange
            var db = DTTestOrganizer.DB() as DTDBDataService;

            //Act
            var mdl = new dtPlanItemModel(DTTestConstants.TestString + "_MinInfo", null, null, null, null, null, null, null, null, null, DTTestConstants.TestUser.id, null, null, null);

            //Assert
            Assert.IsNotNull(mdl);
            Assert.AreEqual(mdl.user.id, DTTestConstants.TestUser.id);
            Assert.AreEqual(mdl.title, DTTestConstants.TestString + "_MinInfo");
        }
    }
}
