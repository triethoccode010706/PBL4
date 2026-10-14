using System.Windows;
using Server.Application;
using Server.Infrastructure;
using Server.Presentation;
using Shared.Protocol;

namespace Server
{
    public partial class App : System.Windows.Application
    {
        private IManagementService? _management;

        private void OnStartup(object sender, StartupEventArgs e)
        {
            var connectionManager = new ConnectionManager();
            var server = new TcpServer(
                5000,
                connectionManager,
                dispatcherFactory: ProtocolServices.CreateServerDispatcher);

            _management = new ManagementService(server, connectionManager);

            var window = new MainWindow(_management);
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _management?.Stop();
            base.OnExit(e);
        }
    }
}
