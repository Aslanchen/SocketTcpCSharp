using SocketTcp.Common;
using SocketTcp.Model;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Windows;
using SocketTcp.Model.Protocl;

namespace SocketTcp
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            TestClient();
            TestServer();
            SocketManager.Instance.IniThread();

            MsgCenter.Instance.RegisterMsg(100, (byteData) =>
             {
                 Test data = Test.Parser.ParseFrom(byteData);
                 if (data == null)
                 {
                     return;
                 }

                 System.Console.WriteLine("收-100 数据-" + data.ToString());
             });
        }

        private void TestClient()
        {
            SocketManager.Instance.IniClient();
        }

        private void TestServer()
        {
            SocketManager.Instance.IniServer();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Test data = new Test()
            {
                Name = "test",
                Address = "This is a Test"
            };
            ByteBuffer buffer = SocketManager.Instance.FormatData(100, data);
            DataModel item = new DataModel(buffer);
            SocketManager.Instance.AddMessage(item);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            List<TcpClient> clients = SocketManager.Instance.GetClients();
            if (clients.Count <= 0)
            {
                return;
            }

            TcpClient client = clients[0];
            Test data = new Test()
            {
                Name = "test",
                Address = "This is a Test"
            };
            ByteBuffer buffer = SocketManager.Instance.FormatData(100, data);
            DataModel item = new DataModel(client, buffer);
            SocketManager.Instance.AddMessage(item);
        }
    }
}
