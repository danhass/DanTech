using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using DanTech.Data;

namespace DanTechTests.Data
{
    public class DTDB
    {
        private static string _conn = "server=162.241.218.73;user id=dimgaard_WPXTY;password=@TheMan001;database=dimgaard_WPXTY;port=3306";
        private dgdb _db = null;

        public dgdb DB { get { return _db; } }

        public DTDB()
        {
            var optionsBuilder = new DbContextOptionsBuilder<dgdb>();
            optionsBuilder.UseMySQL(_conn);
            _db = new dgdb(optionsBuilder.Options);
        }

        public static dgdb getDB()
        {
            var optionsBuilder = new DbContextOptionsBuilder<dgdb>();
            optionsBuilder.UseMySQL(_conn);
            var db = new dgdb(optionsBuilder.Options);
            if (!SetUpDB(db)) throw new Exception("Cannot set up db");
            return db;
        }

        public static bool SetUpDB(dgdb db)
        {
            if (db == null) return false;

            var googleCode = (from x in db.dtTestData where x.title == DTTestConstants.TestGoogleCodeTitle select x).FirstOrDefault();
            var replacement = (from x in db.dtMiscs where x.title == DTTestConstants.TestGoogleCodeMistTitle select x).FirstOrDefault();

            if (googleCode == null)
            {
                if (replacement == null) return false;
                googleCode = new dtTestDatum() { title = "Google code", value = replacement.value };
                db.dtTestData.Add(googleCode);
            }
            else
            {
                if (replacement == null)
                {
                    replacement = new dtMisc() { title = "Google code - testing", value = googleCode.value };
                    db.dtMiscs.Add(replacement);
                }
            }

            var testUser = (from x in db.dtUsers where x.email == DTTestConstants.TestUserEmail select x).FirstOrDefault();
            if (testUser == null)
            {
                testUser = new dtUser() { email = DTTestConstants.TestUserEmail, fName = DTTestConstants.TestUserFName, lName = DTTestConstants.TestUserLName, otherName = DTTestConstants.TestUserOthername };
                db.dtUsers.Add(testUser);
            }

            var testFlag = (from x in db.dtTestData where x.title == "Testing in progress" select x).FirstOrDefault();
            if (testFlag == null)
            {
                testFlag = new dtTestDatum() { title = "Testing in progress", value = "1" };
                db.dtTestData.Add(testFlag);
            }

            db.SaveChanges();

            return true;
        }
    }
}
