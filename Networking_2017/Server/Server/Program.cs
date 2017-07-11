using System;

namespace Server
{
    internal class Program
    {
        private static Server server;

        private static void Main(string[] args)
        {
            server = new Server();
            Console.WriteLine("Run Server?");
            Console.ReadLine();
            server.Run();
            Console.ReadLine();
            Console.WriteLine("Shutting Down Server.");
            server.Shutdown();
            Console.ReadLine();
        }
    }
}