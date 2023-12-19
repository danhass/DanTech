using DanTech.Data;
using DanTech.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DanTechDBTests.LowLevel
{
    [TestClass]
    public class DTDBEntitiesTests
    {
        [TestMethod]
        public void DB_ProperlyInstantiated()
        {
            //Arrange
            var svc = DTTestOrganizer.DB() as DTDBDataService;
            var db = svc.db() as dtdb;

            //Assert
            Assert.IsNotNull(db);
            Assert.IsNotNull(svc.dtdb());
        }

        [TestMethod]
        public void DB_CollectionsExist()
        {
            //Arrange
            var svc = DTTestOrganizer.DB() as DTDBDataService;
            var db = svc.db() as dtdb;

            //Assert
            Assert.IsNotNull(db.dtRegistrations, "db's dtRegistrations is null");
            Assert.IsNotNull(db.dtAuthorizations, "db's dtAuthorizations is null");
            Assert.IsNotNull(db.dtColorCodes, "db's dtColorCodes is null");
            Assert.IsNotNull(db.dtConfigs, "db's dtConfigs is null");
            Assert.IsNotNull(db.dtKeys, "db's dtKeys is null");
            Assert.IsNotNull(db.dtMiscs, "db's dtMiscs is null");
            Assert.IsNotNull(db.dtPlanItems, "db's dtPlanItems is null");
            Assert.IsNotNull(db.dtProjects, "db's dtProjects is null");
            Assert.IsNotNull(db.dtRecurrences, "db's dtRecurrences is null");
            Assert.IsNotNull(db.dtSessions, "db's dtSessions is null");
            Assert.IsNotNull(db.dtStatuses, "db's dtStatuses is null");
            Assert.IsNotNull(db.dtTestData, "db's dtTestData is null");
            Assert.IsNotNull(db.dtTypes, "db's dtTypes is null");
            Assert.IsNotNull(db.dtUsers, "db's dtUsers is null");
        }
    }
}