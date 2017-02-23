using SocketTcp.Common;
using SocketTcp.Model;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Windows;

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
            ByteBuffer buffer = SocketManager.Instance.FormatData(100, "This is a Test");
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
            ByteBuffer buffer = SocketManager.Instance.FormatData(100, "This is a Test");
            DataModel item = new DataModel(client, buffer);
            SocketManager.Instance.AddMessage(item);
        }
    }
}
