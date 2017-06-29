using System;

namespace Client
{
    internal class Program
    {
        private static Client client;

        private static void Main(string[] args)
        {
            client = new Client();
            client.RegisterOK += Client_RegisterOK;
            client.RegisterFailed += Client_RegisterFailed;
            Console.ReadLine();
            client.Register("TestName1", "TestPass1");
            Console.ReadLine();
        }

        private static void Client_RegisterFailed(object sender, IMErrorEventArgs e)
        {
            Console.WriteLine("[{0}] Register Failed.", DateTime.Now);
        }

        private static void Client_RegisterOK(object sender, EventArgs e)
        {
            Console.WriteLine("[{0}] Register OK.", DateTime.Now);
        }
    }
}