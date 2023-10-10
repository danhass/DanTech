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
        public static string ConnectionStringSegment = "DG";
        public const string TestElementKey = "Test data element";
        public static string TestString = "DTDB Test String";
        public static string TestString2 = "Another DTDB Test String";
        public static string TestString3 = "Yet another DTDB Test String";
        public static string TestString4 = "Even another DTDB Test String";
        public static string TestUserEmail = "test@test.com";
        public static dtPlanItem? TestPlanItem = null;
        public static dtProject? TestProject = null;
        public static dtUser? TestUser = null;
        public static Guid _testGuid = Guid.Empty;
        public const string TestProjectTitlePrefix = "Test project #";
        public const string TestStatus = "test";
        public const string TestStringTrueValue = "1";
        public const string TestTimeSpanStart = "14:10";
        public const string TestTimeSpanEnd = "16:15";

        public static Guid TestGuid() { if (_testGuid == Guid.Empty) _testGuid = Guid.NewGuid(); return _testGuid; }

    }
}
