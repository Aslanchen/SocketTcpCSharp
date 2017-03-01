using SocketTcp.Common;
using System;
using System.IO;
using System.Net.Sockets;

namespace SocketTcp.Client
{
    public class SocketClient
    {
        public bool isConnected = false;

        private TcpClient client = null;
        private NetworkStream outStream = null;
        private MemoryStream memStream;
        private BinaryReader reader;

        private const int MAX_READ = 8192;
        private byte[] byteBuffer = new byte[MAX_READ];

        #region 事件
        /// <summary>
        /// 连接事件
        /// </summary>
        public event EventHandler<AsyncEventArgsClient> ServerConnected;
        /// <summary>
        /// 连接异常事件
        /// </summary>
        public event EventHandler<AsyncEventArgsClient> ServerConnectedException;
        /// <summary>
        /// 连接断开事件
        /// </summary>
        public event EventHandler<AsyncEventArgsClient> ServerDisconnected;
        /// <summary>
        /// 接收到数据事件
        /// </summary>
        public event EventHandler<AsyncEventArgsClient> DataReceived;
        /// <summary>
        /// 写异常事件
        /// </summary>
        public event EventHandler<AsyncEventArgsClient> WriteException;

        /// <summary>
        /// 连接事件
        /// </summary>
        private void RaiseServerConnected()
        {
            if (ServerConnected != null)
            {
                ServerConnected(this, new AsyncEventArgsClient());
            }
        }

        /// <summary>
        /// 连接异常事件
        /// </summary>
        private void RaiseServerConnectedException(Exception ex)
        {
            if (ServerConnectedException != null)
            {
                ServerConnectedException(this, new AsyncEventArgsClient(ex));
            }
        }

        /// <summary>
        /// 连接断开事件
        /// </summary>
        private void RaiseServerDisconnected()
        {
            if (ServerDisconnected != null)
            {
                ServerDisconnected(this, new AsyncEventArgsClient());
            }
        }

        /// <summary>
        /// 收到数据事件
        /// </summary>
        /// <param name="buffer"></param>
        private void RaiseDataReceived(ByteBuffer buffer)
        {
            if (DataReceived != null)
            {
                DataReceived(this, new AsyncEventArgsClient(buffer));
            }
        }

        /// <summary>
        /// 写异常事件
        /// </summary>
        /// <param name="ex"></param>
        private void RaiseWriteException(Exception ex)
        {
            if (WriteException != null)
            {
                WriteException(this, new AsyncEventArgsClient(ex));
            }
        }
        #endregion

        // Use this for initialization
        public SocketClient()
        {
            memStream = new MemoryStream();
            reader = new BinaryReader(memStream);
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        public void ConnectServer(string host, int port)
        {
            client = null;
            client = new TcpClient();
            client.SendTimeout = 1000;
            client.ReceiveTimeout = 1000;
            client.NoDelay = true;
            client.BeginConnect(host, port, new AsyncCallback(OnConnect), null);
        }

        /// <summary>
        /// 连接上服务器
        /// </summary>
        private void OnConnect(IAsyncResult asr)
        {
            try
            {
                outStream = client.GetStream();
                outStream.BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), null);
            }
            catch (Exception ex)
            {
                RaiseServerConnectedException(ex);
                return;
            }

            isConnected = true;
            RaiseServerConnected();
        }

        /// <summary>
        /// 写数据
        /// </summary>
        private void WriteMessage(byte[] message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Position = 0;
                BinaryWriter writer = new BinaryWriter(ms);
                writer.Write(message);
                writer.Flush();
                if (client != null && client.Connected)
                {
                    byte[] payload = ms.ToArray();
                    outStream.BeginWrite(payload, 0, payload.Length, new AsyncCallback(OnWrite), null);
                }
                else
                {
                    RaiseWriteException(new Exception("连接断开"));
                }
            }
        }

        /// <summary>
        /// 读取消息
        /// </summary>
        private void OnRead(IAsyncResult asr)
        {
            int bytesRead = 0;
            try
            {
                lock (client.GetStream())
                {
                    //读取字节流到缓冲区
                    bytesRead = client.GetStream().EndRead(asr);
                }
                if (bytesRead < 1)
                {
                    //包尺寸有问题，断线处理
                    RaiseServerDisconnected();
                    return;
                }
                OnReceive(byteBuffer, bytesRead);   //分析数据包内容，抛给逻辑层
                lock (client.GetStream())
                {
                    //分析完，再次监听服务器发过来的新消息
                    Array.Clear(byteBuffer, 0, byteBuffer.Length);   //清空数组
                    client.GetStream().BeginRead(byteBuffer, 0, MAX_READ, new AsyncCallback(OnRead), null);
                }
            }
            catch (Exception)
            {
                RaiseServerDisconnected();
            }
        }

        /// <summary>
        /// 打印字节
        /// </summary>
        private void PrintBytes()
        {
            string returnStr = string.Empty;
            for (int i = 0; i < byteBuffer.Length; i++)
            {
                returnStr += byteBuffer[i].ToString("X2");
            }
            Console.WriteLine(returnStr);
        }

        /// <summary>
        /// 向链接写入数据流
        /// </summary>
        private void OnWrite(IAsyncResult ar)
        {
            try
            {
                outStream.EndWrite(ar);
            }
            catch (Exception ex)
            {
                RaiseWriteException(ex);
            }
        }

        /// <summary>
        /// 接收到消息
        /// </summary>
        private void OnReceive(byte[] bytes, int length)
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
                    writer.Write(reader.ReadBytes(messageLen));
                    ms.Seek(0, SeekOrigin.Begin);
                    OnReceivedMessage(ms);
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
        private void OnReceivedMessage(MemoryStream ms)
        {
            BinaryReader r = new BinaryReader(ms);
            byte[] message = r.ReadBytes((int)ms.Length);

            ByteBuffer buffer = new ByteBuffer(message);
            RaiseDataReceived(buffer);
        }

        /// <summary>
        /// 关闭链接
        /// </summary>
        public void Close()
        {
            if (client != null)
            {
                if (client.Connected) client.Close();
                client = null;
            }
            isConnected = false;

            reader.Close();
            memStream.Close();
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public void SendMessage(ByteBuffer buffer)
        {
            WriteMessage(buffer.ToBytes());
            buffer.Close();
        }
    }
}
