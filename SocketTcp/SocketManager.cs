using System.Collections.Generic;
using System;
using SocketTcp.Client;
using SocketTcp.Common;
using SocketTcp.Server;

namespace SocketTcp
{
    public class SocketManager : Singleton<SocketManager>
    {
        private SocketClient client;
        private SocketServer server;
        private static readonly object m_lockObject = new object();
        private static Queue<KeyValuePair<ushort, byte[]>> mEvents = new Queue<KeyValuePair<ushort, byte[]>>();

        public void IniClient()
        {
            client = new SocketClient();
            client.ServerConnected += Client_ServerConnected;
            client.ServerDisconnected += Client_ServerDisconnected;
            client.DataReceived += Client_DataReceived;
            client.OtherException += Client_OtherException;
            client.WriteException += Client_WriteException;
            client.ConnectServer("127.0.0.1", 6666);
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
            Console.WriteLine("Client_DataReceived");
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
            Console.WriteLine("Server_DataReceived");
        }

        private void Server_ClientDisconnected(object sender, AsyncEventArgsServer e)
        {
            Console.WriteLine("Server_ClientDisconnected");
        }

        private void Server_ClientConnected(object sender, AsyncEventArgsServer e)
        {
            Console.WriteLine("Server_ClientConnected");
        }

        ///------------------------------------------------------------------------------------
        public void AddEvent(ushort _event, byte[] data)
        {
            lock (m_lockObject)
            {
                Console.WriteLine("OnReceive:" + _event);
                mEvents.Enqueue(new KeyValuePair<ushort, byte[]>(_event, data));
            }
        }

        /// <summary>
        /// 交给Command，这里不想关心发给谁。
        /// </summary>
        public void Update()
        {
            if (mEvents.Count > 0)
            {
                while (mEvents.Count > 0)
                {
                    KeyValuePair<ushort, byte[]> _event = mEvents.Dequeue();
                    MsgCenter.Instance.OnMsg(_event.Key, _event.Value);
                }
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        public void Close()
        {
            client.Close();
            server.Stop();
        }


        //public void FormatData(ushort type, IMessage message)
        //{
        //    Console.WriteLine(String.Format("发-{0} 数据-{1}", type, message == null ? "无数据" : message.ToString()));
        //    if (message == null)
        //    {
        //        WriteInt(2);
        //        WriteShort(type);
        //    }
        //    else
        //    {
        //        byte[] data = message.ToByteArray();
        //        WriteInt((uint)(2 + data.Length));
        //        WriteShort(type);
        //        WriteBytes(data);
        //    }
        //}
    }
}