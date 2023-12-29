using DanTech.Data;
using DTNutrition.Services;

namespace DTNutritionTests
{
    [TestClass]
    public class DTNutritionServiceTests
    {
        [TestMethod]
        public void DTNutritionService_Instantiate()
        {
            //Act
            DTNutritionService svc = new DTNutritionService(DTTestOrganizer.DB()!);

            //Assert
            Assert.IsNotNull(svc); ;
        }
        [TestMethod]
        public void DTNutritionService_SetValid()
        {
            //Arrange
            DTNutritionService svc = new DTNutritionService(DTTestOrganizer.DB()!);
            var db = DTTestOrganizer.DB();
            dtFood newFood = new dtFood();
            newFood.title = "New Food Title 1";
            newFood.servingSize = 100;
            newFood.unitType = 1;
            newFood.owner = DTTestConstants.TestKnownGoodUser.id;

            //Act
            var resultFood = svc.Set(newFood);

            //Assert
            Assert.IsNotNull(resultFood);
            Assert.IsTrue(resultFood.id > 0);
            Assert.IsTrue(resultFood.title == "New Food Title 1");
            Assert.IsTrue(resultFood.owner == DTTestConstants.TestKnownGoodUser.id);

            //Clean up
            if (resultFood != null) db.Delete(resultFood);
        }
        [TestMethod]
        public void DTNutritionService_SetNoDB()
        {
            DTNutritionService svc = new DTNutritionService();
            var db = DTTestOrganizer.DB();
            dtFood newFood = new();
            newFood.title = "New Food Title 1";
            newFood.servingSize = 100;
            newFood.unitType = 1;
            newFood.owner = DTTestConstants.TestKnownGoodUser.id;
     
            //Act
            var resultFood = svc.Set(newFood);
 
            //Assert
            Assert.IsNull(resultFood);

            //Clean up
            if (resultFood != null) db.Delete(resultFood);
        }
        [TestMethod]
        public void DTNutritionService_SetNoTitle()
        {
            //Arrange
            DTNutritionService svc = new DTNutritionService(DTTestOrganizer.DB()!);
            var db = DTTestOrganizer.DB();
            dtFood newFood = new();
            newFood.title = string.Empty;
            newFood.servingSize = 100;
            newFood.unitType = 1;
            newFood.owner = DTTestConstants.TestKnownGoodUser.id;

            //Act
            var resultFood = svc.Set(newFood);

            //Assert
            Assert.IsNull(resultFood);

            //Clean up
            if (resultFood != null) db.Delete(resultFood);
        }
        public void DTNutritionService_DuplicateTitle()
        {
            //Arrange
            DTNutritionService svc = new DTNutritionService(DTTestOrganizer.DB()!);
            var db = DTTestOrganizer.DB();
            dtFood newFood = new();
            newFood.title = "Dup Foot Test - Title 1";
            newFood.servingSize = 100;
            newFood.unitType = 1;
            newFood.owner = DTTestConstants.TestKnownGoodUser.id;
            dtFood dupFood = new();
            dupFood.title = "Dup Foot Test - Title 1";
            dupFood.servingSize = 50;
            dupFood.unitType = 2;
            dupFood.owner = DTTestConstants.TestKnownGoodUser.id;

            //Act
            var resultFood = svc.Set(newFood);
            var dupResultFood = svc.Set(dupFood);

            //Assert
            Assert.IsNull(dupResultFood);
            Assert.IsNotNull(resultFood);
            Assert.IsTrue(resultFood.id > 0);
            Assert.IsTrue(resultFood.title == "New Food Title 1");
            Assert.IsTrue(resultFood.owner == DTTestConstants.TestKnownGoodUser.id);

            //Clean up
            if (resultFood != null) db.Delete(resultFood);
            if (dupResultFood != null) db.Delete(dupResultFood);
        }
    }
}