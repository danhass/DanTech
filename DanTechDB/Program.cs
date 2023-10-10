using DanTech.Data;
using DanTech.Services;
using Microsoft.Extensions.Configuration;

namespace DantechDB
{
    public class Program
    {
        private static IDTDBDataService? _db = null;
        public static void Main(string[] args)
        {
            var bldr = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");
            var config = bldr.Build();

            _db = new DTDBDataService(config.GetConnectionString("DG")!);

            Console.WriteLine("There are " + _db.ColorCodes.Count + " color codes");

        }
    }
}
