using System;
using SocketTcp.Client;
using SocketTcp.Common;
using SocketTcp.Server;
using SocketTcp.Model;
using System.Text;
using System.Collections.Generic;
using System.Net.Sockets;
using Google.Protobuf;

namespace SocketTcp
{
    public class SocketManager : Singleton<SocketManager>
    {
        private SocketClient client;
        private SocketServer server;
        private OutThread threadOut;
        private InThread threadIn;

        public void IniClient()
        {
            client = new SocketClient();
            client.ServerConnected += Client_ServerConnected;
            client.ServerConnectedException += Client_ServerConnectedException;
            client.ServerDisconnected += Client_ServerDisconnected;
            client.DataReceived += Client_DataReceived;
            client.OtherException += Client_OtherException;
            client.WriteException += Client_WriteException;
            client.ConnectServer("127.0.0.1", 6666);
        }

        private void Client_ServerConnectedException(object sender, AsyncEventArgsClient e)
        {
            Console.WriteLine("Client_ServerConnectedException");
        }

        private void Client_WriteException(object sender, AsyncEventArgsClient e)
        {
            Console.WriteLine("Client_WriteException");
        }

        private void Client_OtherException(object sender, AsyncEventArgsClient e)
        {
            Console.WriteLine("Client_OtherException");
        }

        private void Client_DataReceived(object sender, AsyncEventArgsClient e)
        {
            DataModel item = new DataModel(e.buffer);
            threadIn.Enqueue(item);
        }

        private void Client_ServerDisconnected(object sender, AsyncEventArgsClient e)
        {
            Console.WriteLine("Client_ServerDisconnected");
        }

        private void Client_ServerConnected(object sender, AsyncEventArgsClient e)
        {
            Console.WriteLine("Client_ServerConnected");
        }

        public void IniServer()
        {
            server = new SocketServer(7777);
            server.ClientConnected += Server_ClientConnected;
            server.ClientDisconnected += Server_ClientDisconnected;
            server.DataReceived += Server_DataReceived;
            server.OtherException += Server_OtherException;
            server.WriteError += Server_WriteError;
            server.Start();
        }

        private void Server_WriteError(object sender, AsyncEventArgsServer e)
        {
            Console.WriteLine("Server_WriteError");
        }

        private void Server_OtherException(object sender, AsyncEventArgsServer e)
        {
            Console.WriteLine("Server_OtherException");
        }

        private void Server_DataReceived(object sender, AsyncEventArgsServer e)
        {
            DataModel item = new DataModel(e.client, e.buffer);
            threadIn.Enqueue(item);
        }

        private void Server_ClientDisconnected(object sender, AsyncEventArgsServer e)
        {
            Console.WriteLine("Server_ClientDisconnected");
        }

        private void Server_ClientConnected(object sender, AsyncEventArgsServer e)
        {
            Console.WriteLine("Server_ClientConnected");
        }

        public void IniThread()
        {
            threadIn = new InThread();
            threadIn.Start();

            threadOut = new OutThread();
            threadOut.Start();
        }

        public void OnMsg(DataModel item)
        {
            Console.WriteLine("OnMsg");
            TcpClient client = item.client;
            ByteBuffer buffer = item.buffer;

            ushort type = buffer.ReadShort();
            byte[] message = buffer.ReadBytes();
            MsgCenter.Instance.OnMsg(type, message);
        }

        public void AddMessage(DataModel item)
        {
            threadOut.Enqueue(item);
        }

        public void SendMessage(DataModel item)
        {
            Console.WriteLine("SendMessage");
            if (item.client == null)
            {
                client.SendMessage(item.buffer);
            }
            else
            {
                server.SendMessage(item.client, item.buffer);
            }
        }

        public List<TcpClient> GetClients()
        {
            return server.clients;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        public void Close()
        {
            client.Close();
            server.Stop();
            threadOut.Stop();
            threadIn.Stop();
        }

        public ByteBuffer FormatData(ushort type, string message)
        {
            ByteBuffer buffer = new ByteBuffer();
            Console.WriteLine(String.Format("发-{0} 数据-{1}", type, message == null ? "无数据" : message));
            if (message == null)
            {
                buffer.WriteInt(2);
                buffer.WriteShort(type);
            }
            else
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                buffer.WriteInt((uint)(2 + data.Length));
                buffer.WriteShort(type);
                buffer.WriteBytes(data);
            }
            return buffer;
        }

        public ByteBuffer FormatData(ushort type, IMessage message)
        {
            ByteBuffer buffer = new ByteBuffer();
            Console.WriteLine(String.Format("发-{0} 数据-{1}", type, message == null ? "无数据" : message.ToString()));
            if (message == null)
            {
                buffer.WriteInt(2);
                buffer.WriteShort(type);
            }
            else
            {
                byte[] data = message.ToByteArray();
                buffer.WriteInt((uint)(2 + data.Length));
                buffer.WriteShort(type);
                buffer.WriteBytes(data);
            }
            return buffer;
        }
    }
}