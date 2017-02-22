using SocketTcp.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace SocketTcp.Server
{
    public class SocketServer
    {
        /// <summary>
        /// 当前的连接的客户端数
        /// </summary>
        private int _clientCount;

        /// <summary>
        /// 客户端会话列表
        /// </summary>
        private List<TCPClientState> _clients;

        private TcpListener listener = null;
        private NetworkStream outStream = null;
        private MemoryStream memStream;
        private BinaryReader reader;

        private const int MAX_READ = 8192;
        private byte[] byteBuffer = new byte[MAX_READ];

        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public bool IsRunning { get; private set; }

        #region 事件
        /// <summary>
        /// 与客户端的连接已建立事件
        /// </summary>
        public event EventHandler<AsyncEventArgsServer> ClientConnected;
        /// <summary>
        /// 与客户端的连接已断开事件
        /// </summary>
        public event EventHandler<AsyncEventArgsServer> ClientDisconnected;
        /// <summary>
        /// 接收到数据事件
        /// </summary>
        public event EventHandler<AsyncEventArgsServer> DataReceived;
        /// <summary>
        /// 写异常事件
        /// </summary>
        public event EventHandler<AsyncEventArgsServer> WriteError;
        /// <summary>
        /// 异常事件
        /// </summary>
        public event EventHandler<AsyncEventArgsServer> OtherException;

        /// <summary>
        /// 触发客户端连接事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseClientConnected(TCPClientState state)
        {
            if (ClientConnected != null)
            {
                ClientConnected(this, new AsyncEventArgsServer(state));
            }
        }

        /// <summary>
        /// 触发客户端连接断开事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseClientDisconnected(TCPClientState state)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(this, new AsyncEventArgsServer(state));
            }
        }

        /// <summary>
        /// 收到数据
        /// </summary>
        /// <param name="state"></param>
        /// <param name="buffer"></param>
        private void RaiseDataReceived(TCPClientState state, ByteBuffer buffer)
        {
            if (DataReceived != null)
            {
                DataReceived(this, new AsyncEventArgsServer(state, buffer));
            }
        }

        /// <summary>
        /// 写异常事件
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ex"></param>
        private void RaiseWriteError(TCPClientState state, Exception ex)
        {
            if (WriteError != null)
            {
                WriteError(this, new AsyncEventArgsServer(state, ex));
            }
        }

        /// <summary>
        /// 触发异常事件
        /// </summary>
        /// <param name="state"></param>
        /// <param name="ex"></param>
        private void RaiseOtherException(TCPClientState state, Exception ex)
        {
            if (OtherException != null)
            {
                OtherException(this, new AsyncEventArgsServer(state, ex));
            }
        }
        #endregion

        public SocketServer(int port)
        {
            _clients = new List<TCPClientState>();
            memStream = new MemoryStream();
            reader = new BinaryReader(memStream);
            listener = new TcpListener(IPAddress.Any, port);
        }

        /// <summary>
        /// 开始监听
        /// </summary>
        public void Start()
        {
            if (IsRunning == true)
            {
                return;
            }

            IsRunning = true;
            listener.Start();
            listener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), null);
        }

        private void AcceptTcpClient(IAsyncResult ar)
        {
            if (IsRunning == false)
            {
                return;
            }

            TcpClient client = listener.EndAcceptTcpClient(ar);
            TCPClientState state = new TCPClientState(client);
            lock (_clients)
            {
                _clients.Add(state);
                _clientCount++;
                RaiseClientConnected(state);
            }

            outStream = client.GetStream();
            outStream.BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), state);
            listener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), null);
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public void SendMessage(TCPClientState state, ByteBuffer buffer)
        {
            WriteMessage(state, buffer.ToBytes());
            buffer.Close();
        }

        /// <summary>
        /// 写数据
        /// </summary>
        private void WriteMessage(TCPClientState state, byte[] message)
        {
            MemoryStream ms = null;
            using (ms = new MemoryStream())
            {
                ms.Position = 0;
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(message);
                writer.Flush();
                if (state.client != null && state.client.Connected)
                {
                    byte[] payload = ms.ToArray();
                    state.NetworkStream.BeginWrite(payload, 0, payload.Length, new AsyncCallback(OnWrite), state);
                }
                else
                {
                    Console.WriteLine("client.connected----->>false");
                }
            }
        }

        /// <summary>
        /// 读取消息
        /// </summary>
        private void OnRead(IAsyncResult ar)
        {
            if (IsRunning == false)
            {
                return;
            }

            TCPClientState state = (TCPClientState)ar.AsyncState;
            NetworkStream stream = state.NetworkStream;

            int bytesRead = 0;
            try
            {
                lock (stream)
                {
                    //读取字节流到缓冲区
                    bytesRead = stream.EndRead(ar);
                }
                if (bytesRead < 1)
                {
                    //包尺寸有问题，断线处理
                    _clients.Remove(state);
                    RaiseClientDisconnected(state);
                    return;
                }
                OnReceive(state, byteBuffer, bytesRead);   //分析数据包内容，抛给逻辑层
                lock (stream)
                {
                    //分析完，再次监听服务器发过来的新消息
                    Array.Clear(byteBuffer, 0, byteBuffer.Length);   //清空数组
                    stream.BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), state);
                }
            }
            catch (Exception ex)
            {
                RaiseOtherException(state, ex);
            }
        }

        /// <summary>
        /// 向链接写入数据流
        /// </summary>
        private void OnWrite(IAsyncResult ar)
        {
            TCPClientState state = (TCPClientState)ar.AsyncState;
            NetworkStream stream = state.NetworkStream;
            try
            {
                stream.EndWrite(ar);
            }
            catch (Exception ex)
            {
                RaiseWriteError(state, ex);
            }
        }

        /// <summary>
        /// 接收到消息
        /// </summary>
        private void OnReceive(TCPClientState state, byte[] bytes, int length)
        {
            memStream.Seek(0, SeekOrigin.End);
            memStream.Write(bytes, 0, length);
            //Reset to beginning
            memStream.Seek(0, SeekOrigin.Begin);
            while (RemainingBytes() > 0)
            {
                MemoryStream ms = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(reader.ReadBytes(length));
                ms.Seek(0, SeekOrigin.Begin);
                OnReceivedMessage(state, ms);
            }
            //Create a new stream with any leftover bytes
            byte[] leftover = reader.ReadBytes((int)RemainingBytes());
            memStream.SetLength(0);     //Clear
            memStream.Write(leftover, 0, leftover.Length);
        }

        /// <summary>
        /// 剩余的字节
        /// </summary>
        private long RemainingBytes()
        {
            return memStream.Length - memStream.Position;
        }

        /// <summary>
        /// 接收到消息
        /// </summary>
        /// <param name="ms"></param>
        private void OnReceivedMessage(TCPClientState state, MemoryStream ms)
        {
            BinaryReader r = new BinaryReader(ms);
            byte[] message = r.ReadBytes((int)ms.Length);
            ByteBuffer buffer = new ByteBuffer(message);
            RaiseDataReceived(state, buffer);
        }

        /// <summary>
        /// 关闭链接
        /// </summary>
        public void Stop()
        {
            if (IsRunning == false)
            {
                return;
            }

            listener.Stop();
            lock (_clients)
            {
                //关闭所有客户端连接
                CloseAllClient();
            }
            IsRunning = false;
            reader.Close();
            memStream.Close();
        }

        private void CloseAllClient()
        {
            foreach (TCPClientState client in _clients)
            {
                Close(client);
            }
            _clientCount = 0;
            _clients.Clear();
        }

        public void Close(TCPClientState state)
        {
            if (state != null)
            {
                state.Close();
                _clients.Remove(state);
                _clientCount--;
            }
        }
    }
}
