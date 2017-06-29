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
        private int m_serverPort = 2000;

        private IPAddress m_serverIP = IPAddress.Parse("127.0.0.1"); //IPAddress.Any;

        private string certFileName = "cert.pfx";
        private string certPassword = "instant";
        private string accountsFileName = "accounts.data";

        // Loaded Data
        private X509Certificate2 m_certificate;

        private AccountDictionary m_accounts;

        // Connections
        private TcpListener m_serverListener;

        //// Task
        //private Thread m_listenThread;

        // private CancellationTokenSource m_listenCancelTS;

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
            Console.WriteLine("[{0}] Server Initalising...", DateTime.Now);
            LoadAccounts();
        }

        public void Run()
        {
            Console.WriteLine("[{0}] Running server...", DateTime.Now);
            m_shouldBeRunning = true;

            // Create Listener
            m_serverListener = new TcpListener(m_serverIP, m_serverPort);
            // Create token
            //m_listenCancelTS = new CancellationTokenSource();
            // Setup shutdown callback
            //m_listenCancelTS.Token.Register(OnResolvedShutdown);
            Console.WriteLine("[{0}] Listening for connections.", DateTime.Now);
            // Start Listening task
            ThreadPool.QueueUserWorkItem(ProcessServer);

            //m_listenThread = Thread. Task.Run(() => ProcessServer(m_listenCancelTS.Token), m_listenCancelTS.Token);
        }

        private void ProcessServer(object _empty)
        {
            m_isRunning = true;
            //List<Task> tasks = new List<Task>();
            //CancellationToken token = (CancellationToken)_token;
            // Start Listening
            m_serverListener.Start();
            while (m_shouldBeRunning)
            {
                // Check if there are any pending connection requests
                if (m_serverListener.Pending())
                {
                    // Accept Connection
                    TcpClient connection = m_serverListener.AcceptTcpClient();
                    // Create a new task to handle the connection
                    ThreadPool.QueueUserWorkItem(ProcessConnection, connection as object);
                    //tasks.Add(Task.Run(() => ProcessConnection(connection)));
                }
            }
            // Stop Listening
            m_serverListener.Stop();

            //// Wait for current tasks to finish, with a timeout.
            //Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(20));

            // Save data
            SaveAccounts();
            m_isRunning = false;
        }

        private void ProcessConnection(object _tcp)
        {
            ClientConnection connection = new ClientConnection(_tcp as TcpClient, m_certificate, m_accounts);
            // Receive state from remote client, then process said state
            connection.SetupConnection();
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
            //m_listenCancelTS.Cancel();
        }

        ///// <summary>
        ///// Shutdown callback, after all remaining connections are resolved and 'accounts data' is saved to file.
        ///// </summary>
        //public void OnResolvedShutdown()
        //{
        //    m_isRunning = false;
        //    Console.WriteLine("[{0}] Shutdown confirmed.", DateTime.Now);
        //}
    }
}