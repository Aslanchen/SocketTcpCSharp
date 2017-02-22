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
        }

        private void TestClient()
        {
            SocketManager.Instance.IniClient();
        }

        private void TestServer()
        {
            SocketManager.Instance.IniServer();
        }
    }
}
