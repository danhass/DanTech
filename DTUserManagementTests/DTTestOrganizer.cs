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

namespace DTUserManagementTests
{
    [TestClass]
    public class DTTestOrganizer
    {
        private static IDTDBDataService? _db = null;
        private static string _conn = String.Empty;
        private static IConfiguration? _cfg = null;
        private static IDTGoogleAuthService? _svc = null;

        [AssemblyInitialize]
        public static void Init(TestContext context)
        {
            Debug.WriteLine(Directory.GetCurrentDirectory());
            var bldr = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.UserManagement.json");
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

            var allTestCodes = _db.Misces.Where(x => x.title == DTTestConstants.TestGoogleCodeKey).OrderByDescending(x => x.id).ToList();
            if (allTestCodes.Count > 0) DTTestConstants.TestGoogleCode = allTestCodes[0].value;
            else DTTestConstants.NoTestGoogleCodes = true;

            _svc = new DTGoogleAuthService();
            _svc.SetConfig(_cfg);

            Debug.WriteLine("Initialized test environment");
        }

        public static IConfiguration? GetConfiguration() { return _cfg; }
        public static IDTDBDataService? DB() { return _db; }
        public static IDTGoogleAuthService? Service() { return _svc; }

 
        [AssemblyCleanup]
        public static void Cleanup()
        {
            var registrations = _db.Registrations.Where(x => x.email == DTTestConstants.TestFictionalEmail 
                                                          || x.email == DTTestConstants.TestTargetEmail 
                                                          || x.email == "register_test_" + DTTestConstants.TestTargetEmail
                                                          ).ToList();
            _db.Delete(registrations);
            foreach(var u in _db.Users.Where(x => x.email == DTTestConstants.TestFictionalEmail).ToList())
            {
                _db.Delete(_db.Sessions.Where(x => x.user == u.id).ToList());
            }
            _db.Delete(_db.Users.Where(x => x.email != DTTestConstants.TestFictionalEmail).ToList());
            Debug.WriteLine("Cleaned up resources used in testing");
        }
    }
}
