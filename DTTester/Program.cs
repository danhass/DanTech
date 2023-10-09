using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using EASendMail;

namespace EASendMailKbCSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("+------------------------------------------------------------------+");
            Console.WriteLine("  Sign in with Google                                             ");
            Console.WriteLine("   If you got \"This app isn't verified\" information in Web Browser, ");
            Console.WriteLine("   click \"Advanced\" -> Go to ... to continue test.");
            Console.WriteLine("+------------------------------------------------------------------+");
            Console.WriteLine("");
            Console.WriteLine("Press any key to sign in...");
            Console.ReadKey();
        }
    }
}
