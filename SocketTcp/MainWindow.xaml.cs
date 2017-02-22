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

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
