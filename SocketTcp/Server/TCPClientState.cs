using System;
using System.Net.Sockets;

namespace SocketTcp.Server
{
    public class TCPClientState
    {
        /// <summary>
        /// 与客户端相关的TcpClient
        /// </summary>
        public TcpClient client { get; set; }

        /// <summary>
        /// 获取网络流
        /// </summary>
        public NetworkStream NetworkStream
        {
            get { return client.GetStream(); }
        }

        public TCPClientState(TcpClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// 关闭
        /// </summary>
        public void Close()
        {
            //关闭数据的接受和发送
            client.Close();
        }
    }
}
