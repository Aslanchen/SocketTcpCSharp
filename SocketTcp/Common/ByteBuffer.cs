using System.IO;
using System.Text;

namespace SocketTcp.Common
{
    public class ByteBuffer
    {
        MemoryStream stream = null;
        BinaryWriter writer = null;
        BinaryReader reader = null;

        public ByteBuffer()
        {
            stream = new MemoryStream();
            writer = new BinaryWriter(stream);
        }

        public ByteBuffer(byte[] data)
        {
            if (data != null)
            {
                stream = new MemoryStream(data);
                reader = new BinaryReader(stream);
            }
            else
            {
                stream = new MemoryStream();
                writer = new BinaryWriter(stream);
            }
        }

        public void Close()
        {
            if (writer != null) writer.Close();
            if (reader != null) reader.Close();

            stream.Close();
            writer = null;
            reader = null;
            stream = null;
        }

        public void WriteInt(uint v)
        {
            writer.Write(v);
        }

        public void WriteShort(ushort v)
        {
            writer.Write(v);
        }

        public void WriteBytes(byte[] v)
        {
            writer.Write(v);
        }

        public void WriteString(string v)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(v);
            writer.Write(bytes);
        }

        public byte ReadByte()
        {
            return reader.ReadByte();
        }

        public ushort ReadShort()
        {
            return reader.ReadUInt16();
        }

        public uint ReadInt()
        {
            return reader.ReadUInt32();
        }

        public string ReadString()
        {
            byte[] buffer = reader.ReadBytes((int)(stream.Length - stream.Position));
            return Encoding.UTF8.GetString(buffer);
        }

        public byte[] ReadBytes(int len)
        {
            return reader.ReadBytes(len);
        }

        public byte[] ToBytes()
        {
            writer.Flush();
            return stream.ToArray();
        }

        public void Flush()
        {
            writer.Flush();
        }
    }
}