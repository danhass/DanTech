using DanTech.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DanTech.Data
{
    public interface IDTDPDAL
    {
        // Temporary methods
        public dgdb GetDB();
        public void SetDB(dgdb db, bool overwrite = false);
        public void Add(dtMisc misc);
        public void Add(dtSession session);
        public dtSession session(dtLogin login);
        public dtSession session(string sessionId);
        public dtSession session(int userId);
        public dtTestDatum testDatum(string title);
        public List<dtType> typesAll();
        public dtUser user(DGDAL_Email email);
    }

    //Supporting classes
    public class DGDAL_Email
    {
        public string Email;
    }
}
