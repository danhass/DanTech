using DanTech.Data;
using DanTech.Data.Models;
using DanTech.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DanTechDBTests.Models
{
    [TestClass]
    public class DTDBColorCodeModelsTests
    {
        [TestMethod]
        public void DTDBColorCodeModels_ProperlyMapped()
        {
            //Arrange
            var svc = DTTestOrganizer.DB() as DTDBDataService;
            var db = svc.db() as dtdb;
            var rawColorCodes = (from x in db.dtColorCodes select x).ToList();
            var allDTOs = svc.ColorCodeDTOs();
            var individualDTOs = new List<dtColorCodeModel>();

            //Act
            foreach (var colorCode in rawColorCodes)
            {
                individualDTOs.Add(svc.ColorCodeDTO(colorCode));
            }

            //Assert
            Assert.AreEqual(rawColorCodes.Count, allDTOs.Count);
            Assert.AreEqual(rawColorCodes.Count, individualDTOs.Count);
            foreach (var colorCode in rawColorCodes)
            {
                var mappedInAll = allDTOs.Where(x => x.id == colorCode.id).FirstOrDefault();
                Assert.IsNotNull(mappedInAll);
                Assert.AreEqual(colorCode.id, mappedInAll.id);
                Assert.AreEqual(colorCode.title, mappedInAll.title);
                Assert.AreEqual(colorCode.note, mappedInAll.note);

                var mappedIndDTO = individualDTOs.Where(x => x.id == colorCode.id).FirstOrDefault();
                Assert.IsNotNull(mappedIndDTO);
                Assert.AreEqual(colorCode.id, mappedIndDTO.id);
                Assert.AreEqual(colorCode.title, mappedIndDTO.title);
                Assert.AreEqual(colorCode.note, mappedIndDTO.note);
            }
        }
    }
}
