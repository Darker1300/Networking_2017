using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Networking;
using Server.Accounts;

namespace Server
{
    public class ClientConnection
    {
        public TcpClient m_clientSocket;        // TCP socket
        public X509Certificate2 m_certificate;  // Reference to security certificate
        public Dictionary<string, AccountData> m_serverAccounts;    // Reference to master accounts data
        public AccountData m_account;           // Reference to associated Account Data in master accounts

        public NetworkStream m_netStream;   // Raw-data stream of connection.
        public SslStream m_ssl;             // Encrypts connection using SSL.
        public BinaryReader m_bReader;      // Read data
        public BinaryWriter m_bWriter;      // Write data

        public ClientConnection(TcpClient _socket, X509Certificate2 _cert, Dictionary<string, AccountData> _accounts)
        {
            m_clientSocket = _socket;
            m_certificate = _cert;
            m_serverAccounts = _accounts;
        }

        public void SetupConnection()
        {
            try
            {
                Logger.Message("New connection detected.");

                // Setup encrypted connection
                SetupSecurity();

                Logger.Message("Connection authenticated.");

                // Send handshake signal.
                m_bWriter.Write(Protocol.IM_Hello);
                m_bWriter.Flush();

                // Wait for Inital Signal
                int handshake = m_bReader.ReadInt32();

                // Handshake Test
                if (handshake != Protocol.IM_Hello)
                {
                    Logger.Message("Handshake Failed.");
                    CloseConnection();
                    return;
                }

                // Handshake packet is OK. Time to wait for login or register.
                byte logMode = m_bReader.ReadByte();
                string userName = m_bReader.ReadString();
                string password = m_bReader.ReadString();

                // Username Length Test
                if (userName.Length >= 10)
                {
                    Logger.Message("Username Lenth Test Failed.");
                    m_bWriter.Write(Protocol.IM_TooUsername);
                    CloseConnection();
                    return;
                }
                // Password Length Test
                if (password.Length >= 20)
                {
                    Logger.Message("Password Lenth Test Failed.");
                    m_bWriter.Write(Protocol.IM_TooPassword);
                    CloseConnection();
                    return;
                }

                // Login Action
                switch (logMode)
                {
                    case Protocol.IM_Register:
                        {
                            // Account Existance Test
                            if (AccountExists(userName))
                            {
                                Logger.Message("Register Error: Account already exists.");
                                // Send Error Message
                                m_bWriter.Write(Protocol.IM_Exists);
                                CloseConnection();
                                return;
                            }
                            // Register
                            CreateAccount(userName, password);
                            // Send Register Success Message to Client
                            m_bWriter.Write(Protocol.IM_OK);
                            m_bWriter.Flush();
                        }
                        break;

                    case Protocol.IM_Login:
                        {
                            // Account Existance Test
                            if (!AccountExists(userName))
                            {
                                Logger.Message("Login Error: Account does not exist.");
                                // Send Error Message
                                m_bWriter.Write(Protocol.IM_NoExists);
                                CloseConnection();
                                return;
                            }
                            // Password Match Test
                            if (!PasswordMatch(userName, password))
                            {
                                Logger.Message("Login Error: Password does not match.");
                                // Send Error Message
                                m_bWriter.Write(Protocol.IM_WrongPass);
                                CloseConnection();
                                return;
                            }
                            // Login
                            Login(userName, password);
                            // Send Login Success Message to Client
                            m_bWriter.Write(Protocol.IM_OK);
                            m_bWriter.Flush();
                        }
                        break;

                    default:
                        Logger.Message("Invalid LoginMode Action ID Received.");
                        CloseConnection();
                        return;
                }

                // Account is logged in.
                lock (m_account) Logger.Message("User logged in: ({0})", m_account.Username);

                try
                {
                    // Listen for client actions.
                    // While client is connected.
                    while (m_clientSocket.Client.Connected)
                    {
                        // Wait for incoming actions
                        byte type = m_bReader.ReadByte();

                        switch (type)
                        {
                            case Protocol.IM_IsAvailable:
                                {
                                    // Wait for targetUsername
                                    string targetUsername = m_bReader.ReadString();

                                    // Prepare start of message
                                    m_bWriter.Write(Protocol.IM_IsAvailable);
                                    m_bWriter.Write(targetUsername);

                                    AccountData targetAccount;
                                    // Account Existance
                                    bool targetExists = GetAccount(targetUsername, out targetAccount);
                                    // Login State
                                    bool targetLoginStatus = targetExists ? GetAccountLoginStatus(targetAccount) : false;

                                    // Availability
                                    bool availability = targetExists && targetLoginStatus;

                                    // Append availability status to message
                                    m_bWriter.Write(availability);

                                    // Send Message
                                    m_bWriter.Flush();
                                }
                                break;

                            case Protocol.IM_Send:
                                {
                                    // Wait for receiver's Username
                                    string receiverUsername = m_bReader.ReadString();
                                    // Wait for receiver's Message
                                    string receiverMessage = m_bReader.ReadString();

                                    AccountData receiverAccount;
                                    // Account Existance
                                    bool receiverExists = GetAccount(receiverUsername, out receiverAccount);
                                    // Login State
                                    bool receiverLoginStatus = receiverExists ? GetAccountLoginStatus(receiverAccount) : false;

                                    if (receiverLoginStatus)
                                    {
                                        lock (receiverAccount)
                                        {
                                            // Prepare start of message to receiver
                                            receiverAccount.Connection.m_bWriter.Write(Protocol.IM_Received);
                                            // Append sender's Username to message
                                            lock (m_account)
                                                receiverAccount.Connection.m_bWriter.Write(m_account.Username);
                                            // Append body of message
                                            receiverAccount.Connection.m_bWriter.Write(receiverMessage);
                                            // Send Message
                                            receiverAccount.Connection.m_bWriter.Flush();

                                            // Message is sent
                                            lock (m_account) Logger.Message("Message sent: ({0} -> {1})", m_account.Username, receiverAccount.Username);
                                        }
                                    }
                                }
                                break;

                            default:
                                // Unknown byte received.
                                // Continue listening.
                                break;
                        }
                    }
                }
                catch (IOException)
                {
                    // User disconnected prematurely?
                    
                    Logger.Message("User has lost connection. ({0})", m_account == null ? "Unknown" : m_account.Username);
                }

                // User disconnected
                lock (m_account)
                    m_account.LoggedIn = false;
            }
            catch
            {
                Logger.Error("Exception Thrown.");
                CloseConnection();
            }

            return; // End
        }

        private void SetupSecurity()
        {
            SetupSecurity(m_clientSocket, m_certificate, out m_netStream, out m_ssl, out m_bReader, out m_bWriter);
        }

        public void CloseConnection()
        {
            CloseConnection(m_clientSocket, m_netStream, m_ssl, m_bReader, m_bWriter);

            if (m_account != null)
                lock (m_account)
                    Logger.Message("Connection Closed. [{0}]", m_account != null ? m_account.Username : "Null");
        }

        private bool AccountExists(string _userName)
        {
            bool exists;
            lock (m_serverAccounts)
                exists = m_serverAccounts.ContainsKey(_userName);
            return exists;
        }

        private bool GetAccountLoginStatus(AccountData _account)
        {
            bool loginStatus = false;
            lock (_account) loginStatus = _account.LoggedIn;
            return loginStatus;
        }

        private bool PasswordMatch(string _userName, string _password)
        {
            bool exists;
            AccountData account;
            bool passMatch;

            lock (m_serverAccounts)
                exists = m_serverAccounts.TryGetValue(_userName, out account);
            if (!exists) return false;

            lock (account)
                passMatch = (_password == account.Password);
            return (passMatch && exists);
        }

        private bool GetAccount(string _userName, out AccountData _account)
        {
            bool exists;
            AccountData account;

            lock (m_serverAccounts)
                exists = m_serverAccounts.TryGetValue(_userName, out account);

            _account = exists ? account : null;
            return exists;
        }

        private void ClearConnection(AccountData _account)
        {
            lock (_account)
            {
                // If user is already logged in, disconnect them.
                if (_account.Connection != null && _account.LoggedIn)
                {
                    _account.Connection.CloseConnection();
                }
                else if (_account.Connection != null)
                    _account.Connection = null;
            }
        }

        private void CreateAccount(string _userName, string _password)
        {
            // Create new account
            m_account = new AccountData(_userName, _password);
            // Set connection
            m_account.Connection = this;
            m_account.LoggedIn = true;

            lock (m_account)
            {
                lock (m_serverAccounts)
                    m_serverAccounts.Add(_userName, m_account);
            }
            Logger.Message("Registered new user ({0})", _userName);
        }

        private void Login(string userName, string password)
        {
            if (m_account != null)
            {
                lock (m_account)
                {
                    // If client is already logged into a different account, disconnect them.
                    if (m_account.LoggedIn && m_account.Connection != this)
                    {
                        m_account.Connection.CloseConnection();
                    }
                    // Set Connection
                    m_account.Connection = this;
                    m_account.LoggedIn = true;
                }
            }
            else
            {
                // Set account
                GetAccount(userName, out m_account);
                // Set connection
                m_account.Connection = this;
                m_account.LoggedIn = true;
            }
        }

        private static void SetupSecurity(TcpClient _socket, X509Certificate2 _certificate, out NetworkStream _netStream, out SslStream _ssl, out BinaryReader _bReader, out BinaryWriter _bWriter)
        {
            _netStream = _socket.GetStream();
            _ssl = new SslStream(_netStream, false);
            _ssl.AuthenticateAsServer(_certificate, false, SslProtocols.Tls, true);

            _bReader = new BinaryReader(_ssl, Encoding.UTF8);
            _bWriter = new BinaryWriter(_ssl, Encoding.UTF8);
            // Connection is now set up and encrypted
        }

        private static void CloseConnection(TcpClient _socket, NetworkStream _netStream, SslStream _ssl, BinaryReader _bReader, BinaryWriter _bWriter)
        {
            _bReader.Close();
            _bWriter.Close();
            _ssl.Close();
            _netStream.Close();
            _socket.Close();
        }
    }
}