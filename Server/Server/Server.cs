using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Server.Accounts;

namespace Server
{
    internal class Server
    {
        // Config
        private int m_serverPort = 8080;
        private IPAddress m_serverIP = IPAddress.Any;

        private static string certFileName = "cert.pfx";
        private static string certPassword = "instant";
        private static string accountsFileName = "users.dat";

        // Loaded Data
        private static X509Certificate2 m_certificate;
        private static HashSet<AccountData> m_accounts;

        // Connections
        private TcpListener m_serverSocket;
        private HashSet<ClientConnection> m_clients;

        // Thread
        private Thread m_serverThread;

        // State
        private bool m_isRunning;

        // Events
        public event EventHandler ServerStart;
        public event EventHandler ServerEnd;

        public Server()
        {
            m_certificate = new X509Certificate2(certFileName, certPassword);

            m_accounts = new HashSet<AccountData>();
            m_clients = new HashSet<ClientConnection>();

            m_isRunning = false;
        }

        public void Shutdown()
        {
            m_isRunning = false;
        }

        public void Run()
        {
            LoadUsers();
            m_isRunning = true;

            m_serverSocket = new TcpListener(m_serverIP, m_serverPort);

            m_serverThread = new Thread(new ThreadStart(ProcessServer));
            m_serverThread.Start();
        }

        private void ProcessServer()
        {
            ServerStart.Invoke(m_serverThread, EventArgs.Empty);

            m_serverSocket.Start(); // Start Listening
            while (m_isRunning)
            {
                // Check if there are any pending connection requests
                if (m_serverSocket.Pending())
                {
                    // Create a new thread to handle the connection
                    TcpClient connection = m_serverSocket.AcceptTcpClient();
                    Thread thread = new Thread(new ParameterizedThreadStart(ProcessConnection));
                    thread.Start(connection as object);
                }
            }
            m_serverSocket.Stop(); // Stop Listening
            SaveUsers(); // Save data

            ServerEnd.Invoke(m_serverThread, EventArgs.Empty);
        }

        private void ProcessConnection(object _obj)
        {
            TcpClient _connection = _obj as TcpClient;
            
            // Get Client ID
            // Make sure AccountData exists
            //
        }

        private void LoadUsers()  // Load users data
        {
            Console.WriteLine("[{0}] Loading users...", DateTime.Now);
            try
            {
                string path = Environment.CurrentDirectory + "\\" + accountsFileName;
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    HashSet<AccountData> Deserial = bf.Deserialize(file) as HashSet<AccountData>;
                    // Merge
                    m_accounts.UnionWith(Deserial);
                }
                Console.WriteLine("[{0}] Users loaded! ({1})", DateTime.Now, m_accounts.Count);
            }
            catch
            {
                Console.WriteLine("[{0}] Users Failed to load!", DateTime.Now);
            }
        }

        private void SaveUsers()  // Save users data
        {
            Console.WriteLine("[{0}] Saving users...", DateTime.Now);
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // Serialize AccountsInfo
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, m_accounts);
                    // Write to file
                    string path = Environment.CurrentDirectory + "\\" + accountsFileName;
                    File.WriteAllBytes(path, ms.ToArray());
                }
                Console.WriteLine("[{0}] Users Saved! ({1})", DateTime.Now, m_accounts.Count);
            }
            catch
            {
                Console.WriteLine("[{0}] Users Failed to save!", DateTime.Now);
            }
        }
    }
}
// TODO wrap connection in class, store connections