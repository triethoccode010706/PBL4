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
            string? secretKey =Environment.GetEnvironmentVariable("PBL4_AUTH_KEY");

            try
            {
                if (string.IsNullOrWhiteSpace(secretKey))
                {
                    throw new ArgumentException(
                        "Chưa cấu hình PBL4_AUTH_KEY.");
                }

                byte[] key = Convert.FromBase64String(secretKey);

                if (key.Length != 32)
                {
                    throw new ArgumentException(
                        "PBL4_AUTH_KEY phải chứa khóa 32 byte.");
                }
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "Thiếu hoặc sai định dạng PBL4_AUTH_KEY.",
                    "Không thể khởi động Server");

                Shutdown(1);
                return;
            }
            var connectionManager = new ConnectionManager();
            var server = new TcpServer(
                5000,
                connectionManager,
                dispatcherFactory: ProtocolServices.CreateServerDispatcher);

            var clientManager = new ClientManager();
            IHeartbeatService heartbeatService =new HeartbeatService(connectionManager,clientManager);
            IAuthenticationService authenticationService = new AuthenticationService(secretKey);
            _management = new ManagementService(
                server,
                connectionManager,
                clientManager,heartbeatService, authenticationService);

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
