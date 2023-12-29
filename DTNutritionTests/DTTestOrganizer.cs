using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using DanTech.Data;
using DanTech.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DTNutritionTests
{
    [TestClass]
    public class DTTestOrganizer
    {
        private static IDTDBDataService? _db = null;
        private static string _conn = String.Empty;
        private static IConfiguration? _cfg = null;

        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            Debug.WriteLine(Directory.GetCurrentDirectory());
            var bldr = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.Nutrition.json");
            var config = bldr.Build();
            _cfg = config;
            _conn = config.GetConnectionString(DTTestConstants.ConnectionStringSegment) ?? string.Empty;

            if (string.IsNullOrEmpty(_conn))
            {
                Debug.WriteLine("Could not find connection string named DT");
            }
            else
            {
                _db = new DTDBDataService(_conn);
                Debug.WriteLine("There are " + _db.ColorCodes.Count + " color codes");
            }

            if (DTTestConstants.TestKnownGoodUser == null) DTTestConstants.TestKnownGoodUser = _db.Users.Where(x => x.email == DTTestConstants.TestKnownGoodUserEmail).FirstOrDefault();

            Debug.WriteLine("Initialized test environment");
        }

        public static IConfiguration? GetConfiguration() { return _cfg; }
        public static IDTDBDataService? DB() { return _db; }
 
        [AssemblyCleanup]
        public static void Cleanup()
        {
            var foods = _db.Foods.Where(x => x.owner == DTTestConstants.TestKnownGoodUser.id).ToList();
            foreach (var food in foods) _db.Delete(_db.FoodLogs.Where(x => x.food == food.id).ToList());
            _db.Delete(foods);            
            
            Debug.WriteLine("Cleaned up resources used in testing");
        }
    }
}
