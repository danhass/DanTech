namespace DanTechTests
{
    public class DTTestConstants
    {
        public const bool TestControl_GetAuthCode_with_code = false;
        public const bool TestControl_EstablishSession_with_code = false;
        public const bool TestControl_SkipAllGoogleAuth = true;
        public const int TryCt = 10;

        public const string AuthTokensNeedToBeResetKey = "Auth tokens need to be reset";
        public const int DefaultNumberOfTestPropjects = 3;
        public const string LocalProtocol = "https";
        public const string LocalHostDomain = "localhost:44324";
        public const string LocatHostIP = "127.0.0.1";
        public const string GoogleSigninHandler = "Home/GoogleSignin";
        public const int RecurrenceId_Daily = 1; //This is not expected to change.
        public const string SessionCookieKey = "dtSessionId";
        public const string TestElementKey = "Test data element";
        public static string? TestGoogleCode = null;
        public static bool NoTestGoogleCodes = false;
        public static readonly string TestGoogleCodeKey = "Google Signin Code";
        public const string TestGoogleCodeTitle = "Google code";
        public const string TestGoogleCodeMistTitle = "Google code - testing";
        public const string TestRemoteHost = "testdomain.com";
        public const string TestRemoteHostAddress = "::1";
        public const string TestKnownGoodUserEmail = "dimgaard@gmail.com";
        public const string TestPlanItemAdditionalTitle = "Additional test plan item title";
        public const string TestPlanItemMinimumTitle = "Minimum test plan item title";       
        public const string TestProjectShortCodePrefix = "TP";
        public const string TestTimeSpanStart = "14:10";
        public const string TestTimeSpanEnd = "16:15";
        public const string TestProjectTitlePrefix = "Test project #";        
        //public const string TestSessionId = "01ddb399-850b-4111-a3e6-6afd1c30b605";
        public const string TestStatus = "test";
        public const string TestStringTrueValue = "1";
        public const string TestUserEmail = "t@t.com"; //Permanent user in the user table that is reserved for testing.
        public const string TestUserFName = "A";
        public const string TestUserLName = "Test";
        public const string TestUserOthername = "tester";
        public const string TestValue = "Test Value";
        public const string TestValue2 = "Test value #2";
        
    }
}
