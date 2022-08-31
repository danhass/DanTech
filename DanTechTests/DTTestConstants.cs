using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using DanTech.Controllers;
using Microsoft.Extensions.DependencyInjection;
using DanTech.Data;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Primitives;
using DanTechTests.Data;
using DanTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace DanTechTests
{
    public class DTTestConstants
    {
        public const bool TestControl_GetAuthCode_with_code = false;
        public const bool TestControl_EstablishSession_with_code = false;
        public const bool TestControl_SkipAllGoogleAuth = true;

        public const string AuthTokensNeedToBeResetKey = "Auth tokens need to be reset";
        public const int DefaultNumberOfTestPropjects = 3;
        public const string LocalProtocol = "https";
        public const string LocalHostDomain = "localhost:44324";
        public const string GoogleSigninHandler = "Home/GoogleSignin";
        public const int RecurranceId_Daily = 1; //This is not expected to change.
        public const string SessionCookieKey = "dtSessionId";
        public const string TestElementKey = "Test data element";
        public const string TestGoogleCodeTitle = "Google code";
        public const string TestGoogleCodeMistTitle = "Google code - testing";
        public const string TestRemoteHost = "testdomain.com";
        public const string TestRemoteHostAddress = "::1";
        public const string TestKnownGoodUserEmail = "hass.dan@gmail.com";
        public const string TestPlanItemAdditionalTitle = "Additional test plan item title";
        public const string TestPlanItemMinimumTitle = "Minimum test plan item title";       
        public const string TestProjectShortCodePrefix = "TP";
        public const string TestTimeSpanStart = "14:10";
        public const string TestTimeSpanEnd = "16:15";
        public const string TestProjectTitlePrefix = "Test project #";        
        public const string TestSessionId = "01ddb399-850b-4111-a3e6-6afd1c30b605";
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
