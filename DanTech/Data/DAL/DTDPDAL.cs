using DanTech.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DanTech.Data
{
    public class DTDPDAL : IDTDPDAL
    {
        private dgdb _DB = null;

        public DTDPDAL(dgdb aDB)
        {
            _DB = aDB;
        }

        public DTDPDAL()
        { }

        public dgdb GetDB()
        {
            return _DB;
        }

        public void SetDB(dgdb db, bool overwrite = false)
        {
            if (db != null && (_DB == null || overwrite)) _DB = db;
        }

        public void Add(dtMisc misc)
        {
            _DB.dtMiscs.Add(misc);
            _DB.SaveChanges();
        }

        public void Add(dtSession session)
        {
            if (session != null)
            {
                _DB.dtSessions.Add(session);
                _DB.SaveChanges();
            }
        }

        public dtSession session(dtLogin login)
        {
            return (from x in _DB.dtSessions where x.session == login.Session select x).FirstOrDefault();
        }

        public dtSession session(string sessionId)
        {
            return (from x in _DB.dtSessions where x.session == sessionId select x).FirstOrDefault();
        }

        public dtSession session(int userId)
        {
            return (from x in _DB.dtSessions where x.user == userId select x).FirstOrDefault();
        }

        public dtTestDatum testDatum(string title)
        {
            return (from x in _DB.dtTestData where x.title == title select x).FirstOrDefault();
        }

        public List<dtType> typesAll()
        {
            return (from x in _DB.dtTypes where 1 == 1 select x).ToList();
        }
        public dtUser user(DGDAL_Email email)
        {
            return (from x in _DB.dtUsers where x.email == email.Email select x).FirstOrDefault();
        }
    }
}
