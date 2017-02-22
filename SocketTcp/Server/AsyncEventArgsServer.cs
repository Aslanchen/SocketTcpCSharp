using SocketTcp.Common;
using System;
using System.Net.Sockets;

namespace SocketTcp.Server
{
    /// <summary>
    /// 异步TcpListener TCP服务器事件参数类 
    /// </summary>
    public class AsyncEventArgsServer : EventArgs
    {
        /// <summary>
        /// 异常
        /// </summary>
        public Exception ex;

        /// <summary>
        /// 接收到的数据
        /// </summary>
        public ByteBuffer buffer;

        /// <summary>
        /// 客户端
        /// </summary>
        public TcpClient client;

        public AsyncEventArgsServer(TcpClient client)
        {
            this.client = client;
        }

        public AsyncEventArgsServer(TcpClient client, ByteBuffer buffer)
        {
            this.client = client;
            this.buffer = buffer;
        }

        public AsyncEventArgsServer(TcpClient client, Exception ex)
        {
            this.client = client;
            this.ex = ex;
        }
    }
}
