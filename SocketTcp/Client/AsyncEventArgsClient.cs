using SocketTcp.Common;
using System;

namespace SocketTcp.Client
{
    /// <summary>
    /// 异步TcpListener TCP服务器事件参数类 
    /// </summary>
    public class AsyncEventArgsClient : EventArgs
    {
        /// <summary>
        /// 异常
        /// </summary>
        public Exception ex;

        /// <summary>
        /// 接收到的数据
        /// </summary>
        public ByteBuffer buffer;


        public AsyncEventArgsClient()
        {
        }

        public AsyncEventArgsClient(Exception ex)
        {
            this.ex = ex;
        }

        public AsyncEventArgsClient(ByteBuffer buffer)
        {
            this.buffer = buffer;
        }
    }
}
