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
        private static dgdb _db = null;

        public dgdb DB { get { return _db; } }

        public DTDB()
        {
            if (_db == null)
            {
                var optionsBuilder = new DbContextOptionsBuilder<dgdb>();
                optionsBuilder.UseMySQL(_conn);
                _db = new dgdb(optionsBuilder.Options);
            }
        }

        public static dgdb getDB(int numberOfTestProjects = 0)
        {
            if (_db == null)
            {
                var optionsBuilder = new DbContextOptionsBuilder<dgdb>();
                optionsBuilder.UseMySQL(_conn);
                var db = new dgdb(optionsBuilder.Options);
                _db = db;
            }
            return _db;
        }
    }
}
