using System;
using System.Net.Sockets;
using System.Threading;
using Server.Accounts;

namespace Server
{
    internal class ClientConnection : IEquatable<ClientConnection>
    {
        public TcpClient m_socket;
        public AccountData m_account;

        public ClientConnection() { }
        public ClientConnection(TcpClient _socket, AccountData _account)
        {
            m_socket = _socket;
            m_account = _account;
        }

        public bool Equals(ClientConnection other)
        {
            return m_account.Equals(other.m_account);
        }

        public override int GetHashCode()
        {
            return m_account.GetHashCode();
        }
    }
}