using Server.Infrastructure;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IServer? _server;
        public MainWindow()
        {
            InitializeComponent();
            StartTcpServer();
        }
            private void StartTcpServer()
        {
            var connectionManager = new ConnectionManager();
            _server = new TcpServer(5000, connectionManager);

            // Chạy server ở luồng nền (Background Task) để không làm đơ giao diện WPF
            _ = Task.Run(async () =>
            {
                await _server.StartAsync();
            });

        }
    }

        
 }