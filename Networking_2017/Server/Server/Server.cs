using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Server.Accounts;
using System.Linq;

namespace Server
{
    // Define types
    using AccountDictionary = Dictionary<string, AccountData>;

    internal class Server
    {
        // Config
        public int m_serverPort = 2000;

        public IPAddress m_serverIP = IPAddress.Parse("127.0.0.1"); //IPAddress.Any;

        private string certFileName = "cert.pfx";
        private string certPassword = "instant";
        private string accountsFileName = "accounts.data";

        // Loaded Data
        private X509Certificate2 m_certificate;

        private AccountDictionary m_accounts;

        // Connections
        private TcpListener m_serverListener;

        List<ClientConnection> m_connections;

        // State
        private bool m_isRunning;
        private bool m_shouldBeRunning;

        public bool IsRunning { get { return m_isRunning; } }

        public Server()
        {
            m_isRunning = false;
            m_shouldBeRunning = false;
            // Create Security Certificate
            m_certificate = new X509Certificate2(certFileName, certPassword);
            // Initalise m_accounts
            LoadAccounts();
        }

        public void Run()
        {
            Console.WriteLine("[{0}] Initialising server.", DateTime.Now);
            m_shouldBeRunning = true;

            // Create Listener
            m_serverListener = new TcpListener(m_serverIP, m_serverPort);
            // Create Connections List
            m_connections = new List<ClientConnection>();

            Console.WriteLine("[{0}] Listening for connections.", DateTime.Now);
            // Start Listening task
            ThreadPool.QueueUserWorkItem(ProcessServer);
        }

        private void ProcessServer(object _empty)
        {
            m_isRunning = true;
            // Start Listening
            m_serverListener.Start();
            while (m_shouldBeRunning)
            {
                // Check if there are any pending connection requests
                if (m_serverListener.Pending())
                {
                    // Accept Connection
                    TcpClient tcpClient = m_serverListener.AcceptTcpClient();
                    // Connect and process remote client state with a different thread
                    ThreadPool.QueueUserWorkItem(ProcessConnection, tcpClient as object);
                }
            }
            // Stop Listening
            m_serverListener.Stop();

            lock (m_connections)
                foreach (ClientConnection cc in m_connections)
                {
                    lock (cc)
                    {
                        cc.CloseConnection();
                    }
                }

            // Save data
            SaveAccounts();
            m_isRunning = false;
        }

        private void ProcessConnection(object _tcp)
        {
            ClientConnection connection = new ClientConnection(_tcp as TcpClient, m_certificate, m_accounts);

            // Add to collection
            lock (m_connections)
                m_connections.Add(connection);

            // Connect and process remote client state
            connection.SetupConnection();

            // Connection has ended
            lock (m_connections)
                m_connections.Remove(connection);
        }
        /// <summary>
        /// Initialises 'm_accounts' with data file at [Environment.CurrentDirectory\accountsFileName].
        /// </summary>
        private void LoadAccounts()
        {
            Console.WriteLine("[{0}] Loading accounts...", DateTime.Now);
            try
            {
                string path = Environment.CurrentDirectory + "\\" + accountsFileName;
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    // Deserialize
                    BinaryFormatter bf = new BinaryFormatter();
                    AccountData[] data = bf.Deserialize(file) as AccountData[];
                    // Apply accounts
                    m_accounts = data.ToDictionary((u) => u.Username, (u) => u);
                }
                Console.WriteLine("[{0}] Accounts loaded. ({1})", DateTime.Now, m_accounts.Count);
            }
            catch
            {
                Console.WriteLine("[{0}] Accounts failed to load.", DateTime.Now);
                m_accounts = new AccountDictionary();
            }
        }

        private void SaveAccounts()
        {
            Console.WriteLine("[{0}] Saving accounts...", DateTime.Now);
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // Serialize accounts
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, m_accounts.Values.ToArray());
                    // Write to file
                    string path = Environment.CurrentDirectory + "\\" + accountsFileName;
                    File.WriteAllBytes(path, ms.ToArray());
                }
                Console.WriteLine("[{0}] Accounts saved. ({1})", DateTime.Now, m_accounts.Count);
            }
            catch
            {
                Console.WriteLine("[{0}] Accounts Failed to save!", DateTime.Now);
            }
        }

        public void Shutdown()
        {
            Console.WriteLine("[{0}] Shutting down...", DateTime.Now);

            m_shouldBeRunning = false;
        }
    }
}