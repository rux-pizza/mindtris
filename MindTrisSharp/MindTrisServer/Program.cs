using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;

namespace MindTrisServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting server...");
			string ip;
			if (args.Length > 0) ip = args[0];
			else ip = "127.0.0.1";
            ip = "138.231.141.213";
            IPAddress addr = IPAddress.Parse(ip);
            Server server = new Server("Behemothzorz 0.1");
            server.Start();
            Console.WriteLine("Hit the shit to stop...");
			Console.ReadLine();
            server.Stop();
        }
    }
}
