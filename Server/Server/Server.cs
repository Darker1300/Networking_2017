using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Server.Accounts;

namespace Server
{
    // Define types
    using AccountDictionary = ConcurrentDictionary<AccountData, AccountData>;
    using ConnectionDictionary = ConcurrentDictionary<ClientConnection, ClientConnection>;

    internal class Server
    {
        // Config
        private int m_serverPort = 8080;

        private IPAddress m_serverIP = IPAddress.Any;

        private string certFileName = "cert.pfx";
        private string certPassword = "instant";
        private string accountsFileName = "users.dat";

        // Loaded Data
        private X509Certificate2 m_certificate;

        private AccountDictionary m_accounts;

        // Connections
        private TcpListener m_serverListener;

        private ConnectionDictionary m_clients;

        // Task
        private Task m_listenTask;

        private CancellationTokenSource m_listenCancelTS;

        // State
        private bool m_isRunning;

        public bool IsRunning { get { return m_isRunning; } }

        public Server()
        {
            m_isRunning = false;
            // Create Security Certificate
            m_certificate = new X509Certificate2(certFileName, certPassword);
            // Initalise m_accounts
            LoadUsers();

            m_clients = new ConnectionDictionary();
            // Create Listener
            m_serverListener = new TcpListener(m_serverIP, m_serverPort);
        }

        public void Shutdown()
        {
            m_listenCancelTS.Cancel();
        }

        public void Run()
        {
            m_isRunning = true;

            // Create token
            m_listenCancelTS = new CancellationTokenSource();
            // Setup shutdown callback
            m_listenCancelTS.Token.Register(OnResolvedShutdown);
            // Start Listening task
            m_listenTask = Task.Run(() => ProcessServer(m_listenCancelTS.Token), m_listenCancelTS.Token);
        }

        private void ProcessServer(CancellationToken _token)
        {
            List<Task> tasks = new List<Task>();

            // Start Listening
            m_serverListener.Start();
            while (!_token.IsCancellationRequested)
            {
                // Check if there are any pending connection requests
                if (m_serverListener.Pending())
                {
                    // Accept Connection
                    TcpClient connection = m_serverListener.AcceptTcpClient();
                    // Create a new task to handle the connection
                    tasks.Add(Task.Run(() => ProcessConnection(connection)));
                }
                m_listenCancelTS.Cancel();
            }
            // Stop Listening
            m_serverListener.Stop();

            // Wait for current tasks to finish, with a timeout.
            Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(20));

            // Save data
            SaveUsers();
        }

        private void ProcessConnection(TcpClient _tcp)
        {
            // Get Client ID
            // Make sure AccountData exists
            //
        }

        /// <summary>
        /// Initialises 'm_accounts' with data file at [Environment.CurrentDirectory\accountsFileName].
        /// </summary>
        private void LoadUsers()
        {
            Console.WriteLine("[{0}] Loading users...", DateTime.Now);
            try
            {
                string path = Environment.CurrentDirectory + "\\" + accountsFileName;
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    // Deserialize
                    BinaryFormatter bf = new BinaryFormatter();
                    var Deserial = bf.Deserialize(file) as KeyValuePair<AccountData, AccountData>[];
                    // Set accounts
                    m_accounts = new AccountDictionary(Deserial);
                }
                Console.WriteLine("[{0}] Users loaded! ({1})", DateTime.Now, m_accounts.Count);
            }
            catch
            {
                Console.WriteLine("[{0}] Users Failed to load!", DateTime.Now);
                m_accounts = new AccountDictionary();
            }
        }

        private void SaveUsers()
        {
            Console.WriteLine("[{0}] Saving users...", DateTime.Now);
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    // Serialize accounts
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, m_accounts.ToArray());
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

        /// <summary>
        /// Shutdown callback, after all remaining connections are resolved and 'accounts data' is saved to file.
        /// </summary>
        private void OnResolvedShutdown()
        {
            m_isRunning = false;
        }

        private void AddOrUpdateConnection()
        {
        }
    }
}