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
        private object lockObject = new object();

        /// <summary>
        /// 客户端会话列表
        /// </summary>
        public List<TcpClient> clients { get; set; }

        private TcpListener listener = null;
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
        /// 触发客户端连接事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseClientConnected(TcpClient client)
        {
            if (ClientConnected != null)
            {
                ClientConnected(this, new AsyncEventArgsServer(client));
            }
        }

        /// <summary>
        /// 触发客户端连接断开事件
        /// </summary>
        /// <param name="state"></param>
        private void RaiseClientDisconnected(TcpClient client)
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected(this, new AsyncEventArgsServer(client));
            }
        }

        /// <summary>
        /// 收到数据
        /// </summary>
        /// <param name="state"></param>
        /// <param name="buffer"></param>
        private void RaiseDataReceived(TcpClient client, ByteBuffer buffer)
        {
            if (DataReceived != null)
            {
                DataReceived(this, new AsyncEventArgsServer(client, buffer));
            }
        }
        #endregion

        public SocketServer(int port)
        {
            clients = new List<TcpClient>();
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

            if (client != null)
            {
                lock (lockObject)
                {
                    clients.Add(client);
                    RaiseClientConnected(client);
                }

                try
                {
                    NetworkStream outStream = client.GetStream();
                    outStream.BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), client);
                }
                catch (Exception)
                {
                    RaiseClientDisconnected(client);
                }
            }

            try
            {
                listener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClient), null);
            }
            catch (Exception)
            {
                Stop();
                Start();
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public void SendMessage(TcpClient client, ByteBuffer buffer)
        {
            WriteMessage(client, buffer.ToBytes());
            buffer.Close();
        }

        /// <summary>
        /// 写数据
        /// </summary>
        private void WriteMessage(TcpClient client, byte[] message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Position = 0;
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(message);
                writer.Flush();

                try
                {
                    byte[] payload = ms.ToArray();
                    client.GetStream().BeginWrite(payload, 0, payload.Length, new AsyncCallback(OnWrite), client);
                }
                catch (Exception)
                {
                    RaiseClientDisconnected(client);
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

            TcpClient client = (TcpClient)ar.AsyncState;
            try
            {
                NetworkStream stream = client.GetStream();

                int bytesRead = 0;
                lock (lockObject)
                {
                    //读取字节流到缓冲区
                    bytesRead = stream.EndRead(ar);
                }
                if (bytesRead < 1)
                {
                    //包尺寸有问题，断线处理
                    clients.Remove(client);
                    RaiseClientDisconnected(client);
                    return;
                }
                OnReceive(client, byteBuffer, bytesRead);   //分析数据包内容，抛给逻辑层
                lock (lockObject)
                {
                    //分析完，再次监听服务器发过来的新消息
                    Array.Clear(byteBuffer, 0, byteBuffer.Length);   //清空数组
                    stream.BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), client);
                }
            }
            catch (Exception)
            {
                RaiseClientDisconnected(client);
            }
        }

        /// <summary>
        /// 向链接写入数据流
        /// </summary>
        private void OnWrite(IAsyncResult ar)
        {
            TcpClient client = (TcpClient)ar.AsyncState;
            try
            {
                NetworkStream stream = client.GetStream();
                stream.EndWrite(ar);
            }
            catch (Exception)
            {
                RaiseClientDisconnected(client);
            }
        }

        /// <summary>
        /// 接收到消息
        /// </summary>
        private void OnReceive(TcpClient client, byte[] bytes, int length)
        {
            memStream.Seek(0, SeekOrigin.End);
            memStream.Write(bytes, 0, length);
            //Reset to beginning
            memStream.Seek(0, SeekOrigin.Begin);
            while (RemainingBytes() > 4)
            {
                int messageLen = reader.ReadInt32();
                if (RemainingBytes() >= messageLen)
                {
                    MemoryStream ms = new MemoryStream();
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write(reader.ReadBytes(length));
                    ms.Seek(0, SeekOrigin.Begin);
                    OnReceivedMessage(client, ms);
                }
                else
                {
                    memStream.Position = memStream.Position - 4;
                    break;
                }
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
        private void OnReceivedMessage(TcpClient client, MemoryStream ms)
        {
            BinaryReader r = new BinaryReader(ms);
            byte[] message = r.ReadBytes((int)ms.Length);
            ByteBuffer buffer = new ByteBuffer(message);
            RaiseDataReceived(client, buffer);
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
            lock (lockObject)
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
            while (clients.Count > 0)
            {
                Close(clients[0]);
            }
        }

        public void Close(TcpClient client)
        {
            if (client != null)
            {
                client.Close();
                clients.Remove(client);
            }
        }
    }
}
