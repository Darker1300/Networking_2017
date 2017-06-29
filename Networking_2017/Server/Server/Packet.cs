using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Networking
{
    internal class Packet
    {
        public Packet(ushort _id, byte[] _contents)
        {
            m_buffer = new byte[OverheadLength + _contents.Length];

            // Copy ID into m_buffer
            ID = _id;
            // Copy Contents Length into m_buffer
            ContentsLength = Convert.ToUInt16(_contents.Length);
            // Copy _contents into m_buffer
            ContentBytes = _contents;
        }

        public Packet(byte[] _packet)
        {
            m_buffer = _packet;
        }

        /// <summary>
        /// The size of overhead in bytes.
        /// </summary>
        public static ushort OverheadLength { get { return sizeof(ushort) * 2; } }

        /// <summary>
        /// Extracts contents from overhead.
        /// </summary>
        public byte[] ContentBytes
        {
            get { return ExtractContents(); }
            set { Buffer.BlockCopy(value, 0, m_buffer, OverheadLength, value.Length); }
        }

        /// <summary>
        /// Converts 'Contents Size'&lt;--&gt;bytes both ways.
        /// </summary>
        public ushort ContentsLength
        {
            get { return BitConverter.ToUInt16(m_buffer, sizeof(ushort)); }
            set { Buffer.BlockCopy(BitConverter.GetBytes((ushort)value), 0, m_buffer, sizeof(ushort), sizeof(ushort)); }
        }

        /// <summary>
        /// Converts PacketID&lt;--&gt;bytes both ways.
        /// </summary>
        public ushort ID
        {
            get { return BitConverter.ToUInt16(m_buffer, 0); }
            set { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, m_buffer, 0, sizeof(ushort)); }
        }

        /// <summary>
        /// This is the full packet, containing both contents and overhead.
        /// </summary>
        public byte[] PacketBytes { get { return m_buffer; } }

        public static explicit operator Packet(byte[] _bytes)
        {
            return new Packet(_bytes);
        }

        public static implicit operator byte[] (Packet _packet)
        {
            return _packet.m_buffer;
        }

        /// <summary>
        /// Serialize Contents.
        /// </summary>
        public static Packet Serialize(ushort _id, object _contents)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, _contents);

                return new Packet(_id, ms.ToArray());
            }
        }

        /// <summary>
        /// Deserialize Contents.
        /// </summary>
        public object Deserialize()
        {
            using (MemoryStream ms = new MemoryStream(PacketBytes, OverheadLength, ContentsLength))
            {
                BinaryFormatter bf = new BinaryFormatter();
                return bf.Deserialize(ms);
            }
        }

        public T Deserialize<T>()
        {
            return (T)Deserialize();
        }

        /// <summary>
        /// Contains both contents and overhead.
        /// </summary>
        private byte[] m_buffer;

        private byte[] ExtractContents()
        {
            ushort size = (ushort)(m_buffer.Length - OverheadLength);
            byte[] c = new byte[size];
            Buffer.BlockCopy(m_buffer, OverheadLength, c, 0, size);
            return c;
        }
    }

    static class Protocol
    {
        public const int IM_Hello = 2012;      // Hello
        public const byte IM_OK = 0;           // OK
        public const byte IM_Login = 1;        // Login
        public const byte IM_Register = 2;     // Register
        public const byte IM_TooUsername = 3;  // Too long username
        public const byte IM_TooPassword = 4;  // Too long password
        public const byte IM_Exists = 5;       // Already exists
        public const byte IM_NoExists = 6;     // Doesn't exists
        public const byte IM_WrongPass = 7;    // Wrong password
        public const byte IM_IsAvailable = 8;  // Is user available?
        public const byte IM_Available = 9;    // User is available or not
        public const byte IM_Send = 10;        // Send message
        public const byte IM_Received = 11;    // Message received
    }
}