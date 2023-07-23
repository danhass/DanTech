using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using DanTech.Data;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace DanTechTests.Data
{
    public class DTDB
    {
        private static string _conn = string.Empty; //"server=162.241.218.73;user id=dimgaard_WPXTY;password=@TheMan001;database=dimgaard_WPXTY;port=3306";
        public static string Conn() {
            if (_conn == string.Empty)
            {
                var lines = File.ReadAllLines(@"G:\My Drive\Projects\DP\api\DanTech\appsettings.json");
                var conn = string.Empty;
                for (int i = 0; i < lines.Length && conn == string.Empty; i++)
                {
                    if (lines[i].Contains("\"DG\": "))
                    {
                        conn = lines[i].Split(":")[1].Substring(2);
                        conn = conn.Substring(0, conn.Length - 1);
                        _conn = conn;
                    }
                }
            }
            return _conn; 
        }
        private static dtdb _db = null;

        public dtdb DB { get { return _db; } }

        public DTDB()
        {
            if (_db == null)
            {
                var optionsBuilder = new DbContextOptionsBuilder<dtdb>();
                optionsBuilder.UseMySQL(_conn);
                _db = new dtdb(optionsBuilder.Options);
            }
        }

        public static dtdb getDB(int numberOfTestProjects = 0)
        {
            if (_db == null)
            {
                var optionsBuilder = new DbContextOptionsBuilder<dtdb>();
                optionsBuilder.UseMySQL(_conn);
                var db = new dtdb(optionsBuilder.Options);
                _db = db;
            }
            return _db;
        }
    }
}
