
using Server.Accounts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;

namespace Server
{
    class Server
    {
        // Server Socket
        TcpListener m_socket;
        IPAddress m_serverIP = IPAddress.Any;
        int m_serverPort = 8080;

        // Server Security
        static X509Certificate2 certificate = null;
        static string certFileName = "cert.pfx";

        // Client Sockets
        static List<ClientData> m_clients;

        static string accountsFileName = "users.dat";
        List<UserInfo> accountsInfo;

        public Server()
        {
            certificate = new X509Certificate2("cert.pfx", "instant");
            accountsInfo = new List<UserInfo>();

        }

        void Start()
        {
            LoadUsers();

            // Create a TCP/IP (IPv4) socket and listen for incoming connections.
            m_socket = new TcpListener(m_serverIP, m_serverPort);
            m_socket.Start();

        }

        void Shutdown()
        {
            SaveUsers();
        }

        public void LoadUsers()  // Load users data
        {
            try
            {
                Console.WriteLine("[{0}] Loading users...", DateTime.Now);
                string path = Environment.CurrentDirectory + "\\" + accountsFileName;
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    accountsInfo.AddRange((UserInfo[])bf.Deserialize(file));   // Deserialize UserInfo array
                }
                Console.WriteLine("[{0}] Users loaded! ({1})", DateTime.Now, accountsInfo.Count);
            }
            catch { }
        }

        public void SaveUsers()  // Save users data
        {
            Console.WriteLine("[{0}] Saving users...", DateTime.Now);
            using (MemoryStream ms = new MemoryStream())
            {
                // Serialize AccountsInfo
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, accountsInfo.ToArray());
                // Write to file
                string path = Environment.CurrentDirectory + "\\" + accountsFileName;
                File.WriteAllBytes(path, ms.ToArray());
            }
            Console.WriteLine("[{0}] Users Saved! ({1})", DateTime.Now, accountsInfo.Count);
        }
    }
}