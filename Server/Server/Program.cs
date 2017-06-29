using System;

namespace Server
{
    internal class Program
    {
        private static Server server;

        private static void Main(string[] args)
        {
            server = new Server();
            server.Run();
            Console.ReadLine();
        }
    }
}