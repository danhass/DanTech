using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DanTech.Data;
using DanTech.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DanTechDBTests
{
    [TestClass]
    public class DTTestOrganizer
    {
        private static IDTDBDataService? _db = null;
        private static string _conn = String.Empty;

        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            var bldr = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.db.json");
            var config = bldr.Build();

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

            if (DTTestConstants.TestUser == null)
            {
                DTTestConstants.TestUser = _db.Users.Where(x => x.email == DTTestConstants.TestUserEmail).FirstOrDefault();
                if (DTTestConstants.TestUser == null)
                {
                    var usr = new dtUser() { email = DTTestConstants.TestUserEmail, fName = "Test", lName = "Test", type = 1 };
                    DTTestConstants.TestUser = _db.Set(usr);
                }
            }

            if (DTTestConstants.TestProject == null)
            {
                DTTestConstants.TestProject = _db.Projects.Where(x => x.title == DTTestConstants.TestString + " (Project)").FirstOrDefault();
                if (DTTestConstants.TestProject == null)
                {
                    var proj = new dtProject() { user = DTTestConstants.TestUser.id, colorCode = 4, shortCode = "TST", priority = 1000, status = 1, title = DTTestConstants.TestString + " (Project)" };
                    DTTestConstants.TestProject = _db.Set(proj);
                }
            }

            if (DTTestConstants.TestPlanItem == null)
            {
                DTTestConstants.TestPlanItem = _db.PlanItems.Where(x => x.title == DTTestConstants.TestString + " (Plan Item)").FirstOrDefault();
                if (DTTestConstants.TestPlanItem == null)
                {
                    var planItem = new dtPlanItem() { title = DTTestConstants.TestString + " (Plan Item)", user = DTTestConstants.TestUser.id, project = DTTestConstants.TestProject.id, day = DateTime.Now, start = DateTime.Now.AddMinutes(30), duration = new TimeSpan(0), priority = 1000 };
                    DTTestConstants.TestPlanItem = _db.Set(planItem);
                }
            }

            Debug.WriteLine("Initialized test environment");
        }

        public static IDTDBDataService? DB() { return _db; }

        [AssemblyCleanup]
        public static void Cleanup()
        {
            _db.Delete(DTTestConstants.TestPlanItem);
            _db.Delete(DTTestConstants.TestProject);
            _db.Delete(DTTestConstants.TestUser);
            Debug.WriteLine("Cleaned up resources used in testing");
        }
    }
}
