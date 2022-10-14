using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DanTech.Data
{
    public class DTConstants
    {
        public const string AuthTokensNeedToBeResetKey = "Auth tokens need to be reset";
        public const string SessionKey = "dtSessionId";
        private static int _timezoneOffset = -10000;
        // Time calclulations are normalized to CST, which is UTC - 5.
        public static int TZOffset { get { if (_timezoneOffset == -10000) _timezoneOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).Hours + 5; return _timezoneOffset; } }
    }
}
