using SocketTcp.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace SocketTcp.Model
{
    public class DataModel
    {
        public TcpClient client { get; set; }
        public ByteBuffer buffer { get; set; }

        public DataModel(TcpClient client, ByteBuffer buffer)
        {
            this.client = client;
            this.buffer = buffer;
        }

        public DataModel(ByteBuffer buffer)
        {
            this.buffer = buffer;
        }
    }
}
