using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class ClientData
    {
        public Socket m_socket;
        public Thread m_thread; // to do

        public string m_id;


        public ClientData()
        {
            m_id = Guid.NewGuid().ToString();
        }

        public ClientData(Socket _socket)
        {
            m_socket = _socket;
            m_id = Guid.NewGuid().ToString();
        }
    }
}
