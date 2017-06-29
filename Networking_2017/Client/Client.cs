using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Networking;

namespace Client
{
    internal class Client
    {
        private TcpClient m_serverSocket;
        private bool _conn = false;    // Is connected/connecting?
        private bool _logged = false;  // Is logged in?
        private string _user;          // Username
        private string _pass;          // Password
        private bool reg;              // Register mode
        
        public NetworkStream netStream; // Raw-data stream of connection.
        public SslStream ssl;           // Encrypts connection using SSL.
        public BinaryReader br;         // Read simple data
        public BinaryWriter bw;         // Write simple data

        public string Server { get { return "localhost"; } }
        public int Port { get { return 2000; } }
        public bool IsLoggedIn { get { return _logged; } }
        public string UserName { get { return _user; } }
        public string Password { get { return _pass; } }

        #region EventHandlers

        public event EventHandler LoginOK;

        public event EventHandler RegisterOK;

        public event IMErrorEventHandler LoginFailed;

        public event IMErrorEventHandler RegisterFailed;

        public event EventHandler Disconnected;

        public event IMAvailEventHandler UserAvailable;

        public event IMReceivedEventHandler MessageReceived;

        #endregion EventHandlers

        /// <summary>
        /// Setup connection and login.
        /// </summary>
        private void SetupConn(object _empty)
        {
            // Setup socket
            m_serverSocket = new TcpClient(Server, Port);

            // Setup Security
            netStream = m_serverSocket.GetStream();
            ssl = new SslStream(netStream, false, new RemoteCertificateValidationCallback(ValidateCert));
            ssl.AuthenticateAsClient("InstantMessengerServer");

            // Connection is now set up and encrypted

            br = new BinaryReader(ssl, Encoding.UTF8);
            bw = new BinaryWriter(ssl, Encoding.UTF8);

            // Do stuff
            // Receive "hello"
            int hello = br.ReadInt32();
            if (hello == Protocol.IM_Hello)
            {
                // Hello OK, so answer.
                bw.Write(Protocol.IM_Hello);

                bw.Write(reg ? Protocol.IM_Register : Protocol.IM_Login);  // Login or register
                bw.Write(UserName);
                bw.Write(Password);
                bw.Flush();

                byte ans = br.ReadByte();  // Read answer.
                if (ans == Protocol.IM_OK)  // Login/register OK
                {
                    if (reg)
                        OnRegisterOK();  // Register is OK.
                    OnLoginOK();  // Login is OK (when registered, automatically logged in)
                    Receiver(); // Time for listening for incoming messages.
                }
                else
                {
                    IMErrorEventArgs err = new IMErrorEventArgs((IMError)ans);
                    if (reg)
                        OnRegisterFailed(err);
                    else
                        OnLoginFailed(err);
                }
            }
            if (_conn)
                CloseConn();
        }

        private void CloseConn() // Close connection.
        {
            br.Close();
            bw.Close();
            ssl.Close();
            netStream.Close();
            m_serverSocket.Close();
            OnDisconnected();
            _conn = false;
        }

        // Start connection thread and login or register.
        private void connect(string user, string password, bool register)
        {
            if (!_conn)
            {
                _conn = true;
                _user = user;
                _pass = password;
                reg = register;

                // Connect and communicate to server in another thread.
                ThreadPool.QueueUserWorkItem(SetupConn);
               // tcpTask = Task.Run((System.Action)SetupConn);
            }
        }

        private void Receiver()  // Receive all incoming packets.
        {
            _logged = true;

            try
            {
                while (m_serverSocket.Connected)  // While we are connected.
                {
                    byte type = br.ReadByte();  // Get incoming packet type.

                    if (type == Protocol.IM_IsAvailable)
                    {
                        string user = br.ReadString();
                        bool isAvail = br.ReadBoolean();
                        OnUserAvail(new IMAvailEventArgs(user, isAvail));
                    }
                    else if (type == Protocol.IM_Received)
                    {
                        string from = br.ReadString();
                        string msg = br.ReadString();
                        OnMessageReceived(new IMReceivedEventArgs(from, msg));
                    }
                }
            }
            catch (IOException) { }

            _logged = false;
        }

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
            if (_conn)
                CloseConn();
        }

        public void IsAvailable(string user)
        {
            if (_conn)
            {
                bw.Write(Protocol.IM_IsAvailable);
                bw.Write(user);
                bw.Flush();
            }
        }

        public void SendMessage(string to, string msg)
        {
            if (_conn)
            {
                bw.Write(Protocol.IM_Send);
                bw.Write(to);
                bw.Write(msg);
                bw.Flush();
            }
        }

        public static bool ValidateCert(object sender, X509Certificate certificate,
              X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true; // Allow untrusted certificates.
        }

        #region Events

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

        virtual protected void OnLoginFailed(IMErrorEventArgs e)
        {
            if (LoginFailed != null)
                LoginFailed(this, e);
        }

        virtual protected void OnRegisterFailed(IMErrorEventArgs e)
        {
            if (RegisterFailed != null)
                RegisterFailed(this, e);
        }

        virtual protected void OnDisconnected()
        {
            if (Disconnected != null)
                Disconnected(this, EventArgs.Empty);
        }

        virtual protected void OnUserAvail(IMAvailEventArgs e)
        {
            if (UserAvailable != null)
                UserAvailable(this, e);
        }

        virtual protected void OnMessageReceived(IMReceivedEventArgs e)
        {
            if (MessageReceived != null)
                MessageReceived(this, e);
        }

        #endregion Events
    }
}