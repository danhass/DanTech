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
        public static Dictionary<int, string> StatusColors = new Dictionary<int, string>();
        private static bool _initialized = false;
        public static bool Initialized() { return _initialized; }
        // Time calclulations are normalized to CST, which is UTC - 5.
        public static int TZOffset { get { if (_timezoneOffset == -10000) 
                                            { _timezoneOffset = -(5 + TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).Hours) + 
                                                (TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now) ? 0 : -1);  
                                            } 
                                            return _timezoneOffset; 
                                        } 
                                    }

        public static void Init(Idtdb db)
        {
            if (db == null) return;
            var _db = db as dtdb;
            var list = (from x in _db.dtStatuses select x).ToList();
            var colors = (from x in _db.dtColorCodes select x).ToList();
            foreach(var l in list)
            {
                if (!StatusColors.Keys.Contains(l.id) && l.colorCode != null)
                {
                    var c = colors.Where(x => x.id == l.colorCode).FirstOrDefault();
                    if (c != null) StatusColors[l.id] = c.title;
                }
            }
            _initialized = true;
        }
    }
}
