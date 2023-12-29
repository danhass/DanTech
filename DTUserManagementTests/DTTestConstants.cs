using DanTech.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTUserManagementTests
{
    public class DTTestConstants
    {
        public static readonly string ConnectionStringSegment = "DG";
        public const string LocatHostIP = "127.0.0.1";
        public static readonly string TestString = "DTDB Test String";
        public static readonly string TestString2 = "Another DTDB Test String";
        public static readonly string TestString3 = "Yet another DTDB Test String";
        public static readonly string TestString4 = "Even another DTDB Test String";
        public static readonly string TestUserEmail = "test@test.com";
        public static readonly string TestKnownGoodUserEmail = "dimgaard@gmail.com";
        public static readonly string TestReturnDomain = "https://localhost:44324";
        public static readonly string TestReturnEndPoint = "Home/SaveGoogleCode";
        public static Guid _testGuid = Guid.Empty;
        public static dtUser? TestKnownGoodUser = null;
        public static string? TestGoogleCode = null;
        public static string? TestGoogleAuth = null;
        public static string? TestGoogleRefresh = null;
        public static bool NoTestGoogleCodes = false;
        public static readonly string TestGoogleCodeKey = "Google Signin Code";
        public static readonly string TestTargetEmail = "hass.dan@gmail.com";
        public static readonly string TestFictionalEmail = "test@test.com";
        public static readonly string TestBaseUrl = @"https://7822-54268.el-alt.com/";


        public static Guid TestGuid() { if (_testGuid == Guid.Empty) _testGuid = Guid.NewGuid(); return _testGuid; }
    }
}
