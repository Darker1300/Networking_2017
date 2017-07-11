using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Networking;

namespace Client
{
    public class Client : IDisposable
    {
        private TcpClient m_serverSocket;
        private bool m_connected = false;  // Is connected/connecting?
        private bool m_connecting = false;  // Is connected/connecting?
        private bool m_logged = false;      // Is logged in?
        private string m_username;          // Username
        private string m_password;          // Password

        /// <summary>
        /// True: Login. False: Register.
        /// </summary>
        private bool m_loginMode;

        private IPAddress m_serverIP = IPAddress.Loopback;
        private int m_serverPort = 2000;

        public NetworkStream m_netStream; // Raw-data stream of connection.
        public SslStream m_ssl;           // Encrypts connection using SSL.
        public BinaryReader m_bReader;    // Read data
        public BinaryWriter m_bWriter;    // Write data

        public string ServerIP { get { return m_serverIP.ToString(); } set { IPAddress.TryParse(value, out m_serverIP); } }
        public int ServerPort { get { return m_serverPort; } set { m_serverPort = value; } }
        public bool IsLoggedIn { get { return m_logged; } }
        public string Username { get { return m_username; } }
        public string Password { get { return m_password; } }

        #region EventHandlers

        public event EventHandler ServerOK;

        public event EventHandler ServerFailed;

        public event EventHandler LoginOK;

        public event ErrorEventHandler LoginFailed;

        public event EventHandler RegisterOK;

        public event ErrorEventHandler RegisterFailed;

        public event EventHandler Disconnected;

        public event AvailEventHandler UserAvailable;

        public event ReceivedEventHandler MessageReceived;

        #endregion EventHandlers

        #region Internals

        /// <summary>
        /// Handle Connection to Server
        /// </summary>
        private void SetupConnection(object _empty)
        {
            // Set up socket connection
            m_serverSocket = new TcpClient();
            try
            {
                m_serverSocket.Connect(m_serverIP, m_serverPort);
            }
            catch (Exception)
            {
                // Could not connect to address
                m_connecting = false;
                return;
            }

            try
            {
                // Set up Security
                m_netStream = m_serverSocket.GetStream();
                m_ssl = new SslStream(m_netStream, false, new RemoteCertificateValidationCallback(ValidateCert));
                m_ssl.AuthenticateAsClient("LoginServer");

                m_bReader = new BinaryReader(m_ssl, Encoding.UTF8);
                m_bWriter = new BinaryWriter(m_ssl, Encoding.UTF8);
            }
            catch (Exception)
            {
                // Could not verify security
                m_serverSocket.Close();
                return;
            }

            // Connection is now set up and encrypted
            m_connected = true;
            m_connecting = false;

            try // Catch IOException, in case server connection unexpectedly terminates.
            {
                // Wait for Server's Initial Handshake Signal
                int handshake = m_bReader.ReadInt32();

                // Handshake Test
                if (handshake != Protocol.IM_Hello)
                {
                    // Logger.Message("Handshake Failed.");
                    if (m_connected)
                        CloseConnection();
                    return;
                }

                // Return Handshake
                m_bWriter.Write(Protocol.IM_Hello);

                // Compose Login Action
                m_bWriter.Write(m_loginMode ? Protocol.IM_Login : Protocol.IM_Register);  // Login or register
                m_bWriter.Write(Username);
                m_bWriter.Write(Password);
                // Send
                m_bWriter.Flush();

                // Wait for Server's Login Action Response
                byte LAResponse = m_bReader.ReadByte();
                switch (LAResponse)
                {
                    case Protocol.IM_OK:
                        {   // Login Action successful
                            // Invoke Events
                            if (!m_loginMode)
                                OnRegisterOK();
                            OnLoginOK();    // (when registered, automatically logged in)
                            break;
                        }

                    default:
                        {
                            // Received error
                            ErrorEventArgs err = new ErrorEventArgs((ErrorID)LAResponse);
                            if (!m_loginMode)
                                OnRegisterFailed(err);
                            else
                                OnLoginFailed(err);

                            //Logger.Message("Invalid LoginMode Action Response ID Received.");
                            if (m_connected)
                                CloseConnection();
                            return;
                        }
                }

                // Account is logged in.
                m_logged = true;

                // Listen for server actions/responses.
                // While server is connected.
                while (m_serverSocket.Connected)
                {
                    // Wait for incoming actions
                    byte type = m_bReader.ReadByte();

                    switch (type)
                    {
                        case Protocol.IM_IsAvailable:
                            {
                                // Wait for target's username
                                string user = m_bReader.ReadString();
                                // Wait for target's availability status
                                bool isAvail = m_bReader.ReadBoolean();
                                // Invoke Event
                                OnUserAvail(new UserAvailEventArgs(user, isAvail));
                            }
                            break;

                        case Protocol.IM_Received:
                            {
                                // Wait for Message's sender username
                                string from = m_bReader.ReadString();
                                // Wait for Message's contents
                                string msg = m_bReader.ReadString();
                                OnMessageReceived(new MsgReceivedEventArgs(from, msg));
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
                // Lost Connection with Server
                if (m_connected)
                    CloseConnection();
                return;
            }

            // Make sure client is disconnected
            if (m_connected)
                CloseConnection();
            return;
        }

        /// <summary>
        /// Close Connection to Server
        /// </summary>
        private void CloseConnection() // Close connection.
        {
            m_bReader.Close();
            m_bWriter.Close();
            m_ssl.Close();
            m_netStream.Close();
            m_serverSocket.Close();
            OnDisconnected();
            m_connected = false;
            m_logged = false;
        }

        /// <summary>
        /// Initiate a new Threaded Connection to Server
        /// </summary>
        private void connect(string user, string password, bool register)
        {
            if (!(m_connecting || m_connected))
            {
                m_connecting = true;
                m_username = user;
                m_password = password;
                m_loginMode = !register;

                // Connect and communicate to server in another thread.
                ThreadPool.QueueUserWorkItem(SetupConnection);
            }
        }

        /// <summary>
        /// Security Validation Callback
        /// </summary>
        private static bool ValidateCert(object sender, X509Certificate certificate,
              X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true; // Allow untrusted certificates.
        }

        /// <summary>
        /// Ensures connection to server is closed upon deallocation
        /// </summary>
        public void Dispose()
        {
            if (m_connected)
                CloseConnection();
        }

        #endregion Internals

        #region Actions

        public void Login(string user, string password)
        {
            connect(user, password, false);
        }

        public void Register(string user, string password)
        {
            connect(user, password, true);
        }

        public void Disconnect()
        {
            if (m_connected)
                CloseConnection();
        }

        public void IsAvailable(string user)
        {
            if (m_connected)
            {
                m_bWriter.Write(Protocol.IM_IsAvailable);
                m_bWriter.Write(user);
                m_bWriter.Flush();
            }
        }

        public void SendMessage(string to, string msg)
        {
            if (m_connected)
            {
                m_bWriter.Write(Protocol.IM_Send);
                m_bWriter.Write(to);
                m_bWriter.Write(msg);
                m_bWriter.Flush();
            }
        }

        #endregion Actions

        #region Events

        virtual protected void OnServerOK()
        {
            if (ServerOK != null)
                ServerOK(this, EventArgs.Empty);
        }

        virtual protected void OnServerFailed()
        {
            if (ServerFailed != null)
                ServerFailed(this, EventArgs.Empty);
        }

        virtual protected void OnLoginOK()
        {
            if (LoginOK != null)
                LoginOK(this, EventArgs.Empty);
        }

        virtual protected void OnRegisterOK()
        {
            if (RegisterOK != null)
                RegisterOK(this, EventArgs.Empty);
        }

        virtual protected void OnLoginFailed(ErrorEventArgs e)
        {
            if (LoginFailed != null)
                LoginFailed(this, e);
        }

        virtual protected void OnRegisterFailed(ErrorEventArgs e)
        {
            if (RegisterFailed != null)
                RegisterFailed(this, e);
        }

        virtual protected void OnDisconnected()
        {
            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);
        }

        virtual protected void OnUserAvail(UserAvailEventArgs e)
        {
            if (UserAvailable != null)
                UserAvailable(this, e);
        }

        virtual protected void OnMessageReceived(MsgReceivedEventArgs e)
        {
            if (MessageReceived != null)
                MessageReceived(this, e);
        }

        #endregion Events
    }
}