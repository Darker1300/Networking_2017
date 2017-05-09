using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Networking
{
    class Packet
    {
        /// <summary>
        /// Contains both contents and overhead.
        /// </summary>
        private byte[] m_buffer;

        /// <summary>
        /// Converts PacketID&lt;--&gt;bytes both ways.
        /// </summary>
        public ushort ID
        {
            get { return BitConverter.ToUInt16(m_buffer, 0); }
            set { Buffer.BlockCopy(BitConverter.GetBytes(value), 0, m_buffer, 0, sizeof(ushort)); }
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
        /// The size of overhead in bytes.
        /// </summary>
        public static ushort OverheadLength { get { return sizeof(ushort) * 2; } }

        /// <summary>
        /// This is the full packet, containing both contents and overhead.
        /// </summary>
        public byte[] PacketBytes { get { return m_buffer; } }

        /// <summary>
        /// Extracts contents from overhead.
        /// </summary>
        public byte[] ContentBytes { get { return ExtractContents(); } }


        public Packet(ushort _id, byte[] _contents)
        {
            m_buffer = new byte[OverheadLength + _contents.Length];

            ID = _id;
            ContentsLength = (ushort)_contents.Length;
            Buffer.BlockCopy(_contents, 0, m_buffer, OverheadLength, _contents.Length);
        }

        public Packet(byte[] _packet)
        {
            m_buffer = _packet;
        }

        private byte[] ExtractContents()
        {
            ushort size = (ushort)(m_buffer.Length - OverheadLength);
            byte[] c = new byte[size];
            Buffer.BlockCopy(m_buffer, OverheadLength, c, 0, size);
            return c;
        }

        public static implicit operator byte[] (Packet _packet)
        {
            return _packet.m_buffer;
        }

        public static explicit operator Packet (byte[] _bytes)
        {
            return new Packet(_bytes);
        }

        /// <summary>
        /// Serialize Contents.
        /// </summary>
        /// <param name="_id"></param>
        /// <param name="_contents"></param>
        /// <returns></returns>
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
        /// <returns></returns>
        public object Deserialize()
        {
            using (MemoryStream ms = new MemoryStream(PacketBytes, OverheadLength, ContentsLength))
            {
                BinaryFormatter bf = new BinaryFormatter();
                return bf.Deserialize(ms);
            }
        }
        public T Deserialize<T>() { return (T)Deserialize(); }

    }
}
