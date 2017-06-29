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
        public AccountData m_account;   // Associated Account Data
        public TcpClient m_clientSocket;    // TCP socket
        public X509Certificate2 m_certificate;
        public Dictionary<string, AccountData> m_serverAccounts;

        public NetworkStream netStream; // Raw-data stream of connection.
        public SslStream ssl;           // Encrypts connection using SSL.
        public BinaryReader br;         // Read simple data
        public BinaryWriter bw;         // Write simple data

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
                Console.WriteLine("[{0}] New connection!", DateTime.Now);
                // Setup Security
                netStream = m_clientSocket.GetStream();
                ssl = new SslStream(netStream, false);
                ssl.AuthenticateAsServer(m_certificate, false, SslProtocols.Tls, true);
                Console.WriteLine("[{0}] Connection authenticated!", DateTime.Now);

                // Connection is now set up and encrypted

                br = new BinaryReader(ssl, Encoding.UTF8);
                bw = new BinaryWriter(ssl, Encoding.UTF8);

                // Say "hello".
                bw.Write(Protocol.IM_Hello);
                bw.Flush();
                int hello = br.ReadInt32();
                if (hello == Protocol.IM_Hello)
                {
                    // Hello packet is OK. Time to wait for login or register.
                    byte logMode = br.ReadByte();
                    string userName = br.ReadString();
                    string password = br.ReadString();

                    if (userName.Length < 10) // Isn't username too long?
                    {
                        if (password.Length < 20)  // Isn't password too long?
                        {
                            if (logMode == Protocol.IM_Register)  // Register mode
                            {
                                if (!m_serverAccounts.ContainsKey(userName))  // User already exists?
                                {
                                    m_account = new AccountData(userName, password, this);
                                    m_serverAccounts.Add(userName, m_account);  // Add new user
                                    bw.Write(Protocol.IM_OK);
                                    bw.Flush();
                                    Console.WriteLine("[{0}] ({1}) Registered new user", DateTime.Now, userName);
                                    // prog.SaveUsers();
                                    Receiver();  // Listen to client in loop.
                                }
                                else
                                    bw.Write(Protocol.IM_Exists);
                            }
                            else if (logMode == Protocol.IM_Login)  // Login mode
                            {
                                if (m_serverAccounts.TryGetValue(userName, out m_account))  // User exists?
                                {
                                    if (password == m_account.Password)  // Is password OK?
                                    {
                                        // If user is logged in yet, disconnect him.
                                        if (m_account.LoggedIn)
                                            m_account.Connection.CloseConnection();

                                        m_account.Connection = this;
                                        bw.Write(Protocol.IM_OK);
                                        bw.Flush();
                                        Receiver();  // Listen to client in loop.
                                    }
                                    else
                                        bw.Write(Protocol.IM_WrongPass);
                                }
                                else
                                    bw.Write(Protocol.IM_NoExists);
                            }
                        }
                        else
                            bw.Write(Protocol.IM_TooPassword);
                    }
                    else
                        bw.Write(Protocol.IM_TooUsername);
                }
                CloseConnection();
            }
            catch
            {
                CloseConnection();
            }
        }

        private void CloseConnection()
        {
            br.Close();
            bw.Close();
            ssl.Close();
            netStream.Close();
            m_clientSocket.Close();
            Console.WriteLine("[{0}] Connection Closed! [{1}]", DateTime.Now, m_account != null ? m_account.Username : "Null");
        }

        private void Receiver()  // Receive all incoming packets.
        {
            Console.WriteLine("[{0}] ({1}) User logged in", DateTime.Now, m_account.Username);
            m_account.LoggedIn = true;

            try
            {
                while (m_clientSocket.Client.Connected)  // While we are connected.
                {
                    byte type = br.ReadByte();  // Get incoming packet type.

                    if (type == Protocol.IM_IsAvailable)
                    {
                        string who = br.ReadString();

                        bw.Write(Protocol.IM_IsAvailable);
                        bw.Write(who);

                        AccountData info;
                        if (m_serverAccounts.TryGetValue(who, out info))
                        {
                            if (info.LoggedIn)
                                bw.Write(true);   // Available
                            else
                                bw.Write(false);  // Unavailable
                        }
                        else
                            bw.Write(false);      // Unavailable
                        bw.Flush();
                    }
                    else if (type == Protocol.IM_Send)
                    {
                        string to = br.ReadString();
                        string msg = br.ReadString();

                        AccountData recipient;
                        if (m_serverAccounts.TryGetValue(to, out recipient))
                        {
                            // Is recipient logged in?
                            if (recipient.LoggedIn)
                            {
                                // Write received packet to recipient
                                recipient.Connection.bw.Write(Protocol.IM_Received);
                                recipient.Connection.bw.Write(m_account.Username);  // From
                                recipient.Connection.bw.Write(msg);
                                recipient.Connection.bw.Flush();
                                Console.WriteLine("[{0}] ({1} -> {2}) Message sent!", DateTime.Now, m_account.Username, recipient.Username);
                            }
                        }
                    }
                }
            }
            catch (IOException) { }

            m_account.LoggedIn = false;
            Console.WriteLine("[{0}] ({1}) User logged out", DateTime.Now, m_account.Username);
        }
    }
}