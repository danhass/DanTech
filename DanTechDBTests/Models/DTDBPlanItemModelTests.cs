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
            var svc = DTTestOrganizer.DB() as DTDBDataService;

            //Act
            var mdl = new dtPlanItemModel(DTTestConstants.TestPlanItem!);

            //Assert
            Assert.IsNotNull(mdl);
            Assert.AreEqual(mdl.title, DTTestConstants.TestPlanItem.title);
            Assert.AreEqual(mdl.id, DTTestConstants.TestPlanItem.id);
        }
    }
}
