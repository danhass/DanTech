using DanTech.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTNutritionTests
{
    public class DTTestConstants
    {
        public static readonly string ConnectionStringSegment = "DG";
        public static readonly string TestString = "DTNutrition Test String";
        public static readonly string TestString2 = "Another DTNutrition Test String";
        public static readonly string TestString3 = "Yet another DTNutrition Test String";
        public static readonly string TestString4 = "Even another DTNutrition Test String";
        public static readonly string TestUserEmail = "test@test.com";
        public static readonly string TestKnownGoodUserEmail = "dimgaard@gmail.com";
        public static Guid _testGuid = Guid.Empty;
        public static dtUser? TestKnownGoodUser = null;

        public static Guid TestGuid() { if (_testGuid == Guid.Empty) _testGuid = Guid.NewGuid(); return _testGuid; }

    }
}
