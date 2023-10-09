using DanTech.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanTechDBTests
{
    public class DTTestConstants
    {
        public static string ConnectionStringSegment = "DT";
        public static string TestString = "DTDB Test String";
        public static string TestString2 = "Another DTDB Test String";
        public static string TestString3 = "Yet another DTDB Test String";
        public static string TestString4 = "Even another DTDB Test String";
        public static string TestUserEmail = "test@test.com";
        public static dtPlanItem? TestPlanItem = null;
        public static dtProject? TestProject = null;
        public static dtUser? TestUser = null;
        public static Guid _testGuid = Guid.Empty;

        public static Guid TestGuid() { if (_testGuid == Guid.Empty) _testGuid = Guid.NewGuid(); return _testGuid; }

    }
}
