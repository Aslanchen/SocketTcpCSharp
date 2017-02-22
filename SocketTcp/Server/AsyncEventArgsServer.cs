using SocketTcp.Common;
using System;

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
        /// 客户端状态封装类
        /// </summary>
        public TCPClientState state;

        public AsyncEventArgsServer(TCPClientState state)
        {
            this.state = state;
        }

        public AsyncEventArgsServer(TCPClientState state, ByteBuffer buffer)
        {
            this.state = state;
            this.buffer = buffer;
        }

        public AsyncEventArgsServer(TCPClientState state, Exception ex)
        {
            this.state = state;
            this.ex = ex;
        }
    }
}
