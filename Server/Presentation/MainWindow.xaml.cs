using System.Windows;
using System.Windows.Controls;
using Server.Application;
using Server.Domain;
using Shared.Models;

namespace Server.Presentation
{
    public partial class MainWindow : Window
    {
        private readonly IManagementService _management;
        private bool _isSending;
        private bool _isClosing;
        private bool _canClose;
        private bool _isClosed;
        private readonly HashSet<string> _removedSessions = new();
        private bool _serverRunning;
        public MainWindow(IManagementService management)
        {
            _management = management;
            InitializeComponent();
            Closed += OnClosed;
            BindApplicationEvents();

            StatusText.Text = "Đang khởi động Server...";
            SendCommandButton.IsEnabled = false;
            _management.Start();
            Closing += OnClosing;
        }

        private void BindApplicationEvents()
        {
            _management.ClientConnected += client =>
            {
                RunOnUiThread(() => AddClient(client));
            };

            _management.ClientDisconnected += sessionId =>
            {
                RunOnUiThread(() => RemoveClient(sessionId));
            };

            _management.CommandSent += (sessionId, command) =>
            {
                RunOnUiThread(() =>
                {
                    AppendLog(
                        $"→ {ShortId(sessionId)}: {command.Type} "
                        + $"(id={command.RequestId})");
                });
            };

            _management.ResponseReceived += (sessionId, response) =>
            {
                RunOnUiThread(() =>
                {
                    string status = response.Success
                        ? "OK"
                        : $"LỖI {response.ErrorCode}";

                    AppendLog(
                        $"← {ShortId(sessionId)}: {status} "
                        + $"{response.Message} {response.Data}");
                });
            };

            _management.ErrorOccurred += message =>
            {
                RunOnUiThread(() =>
                {
                    AppendLog("Lỗi Server: " + message);
                });
            };

            _management.ClientUpdated += client =>
            {
                RunOnUiThread(() => UpdateClient(client));
            };

            _management.ServerStarted += () =>
            {
                RunOnUiThread(() =>
                {
                    if (_isClosing)
                    {
                        return;
                    }

                    _serverRunning = true;

                    StatusText.Text =
                        "Máy quản lý đang lắng nghe cổng 5000";

                    SendCommandButton.IsEnabled = true;

                    AppendLog("Server đã khởi động.");
                });
            };
            _management.ServerStartFailed += message =>
            {
                RunOnUiThread(() =>
                {
                    _serverRunning = false;
                    SendCommandButton.IsEnabled = false;

                    if (!_isClosing)
                    {
                        StatusText.Text = "Khởi động Server thất bại";
                    }

                    AppendLog("Không thể khởi động Server: " + message);
                });
            };
        }
        private void RunOnUiThread(Action action)
        {
            if (Dispatcher.HasShutdownStarted ||
                Dispatcher.HasShutdownFinished)
            {
                return;
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_isClosed)
                {
                    return;
                }

                action();
            }));
        }
        private void AddClient(ConnectedClient client)
        {
            if (_removedSessions.Contains(client.SessionId))
            {
                return;
            }

            foreach (ListBoxItem item in ClientList.Items)
            {
                if (item.Tag as string == client.SessionId)
                {
                    return;
                }
            }
            ClientList.Items.Add(new ListBoxItem
            {
                Content = client.ToString(),
                Tag = client.SessionId
            });

            if (ClientList.SelectedItem == null)
            {
                ClientList.SelectedIndex = 0;
            }

            AppendLog($"Client kết nối: {client.SessionId} từ {client.RemoteEndPoint}");
        }

        private void RemoveClient(string sessionId)
        {
            _removedSessions.Add(sessionId);
            object? item = ClientList.Items
                .Cast<object>()
                .FirstOrDefault(x => x is ListBoxItem listItem
                    && string.Equals(listItem.Tag as string, sessionId, StringComparison.Ordinal));
            if (item != null)
            {
                ClientList.Items.Remove(item);
            }

            AppendLog($"Client ngắt kết nối: {sessionId}");
        }

        private async void OnSendCommandClick(object sender, RoutedEventArgs e)
        {
            if (_isSending || _isClosing || !_serverRunning)
            {
                return;
            }

            if (ClientList.SelectedItem is not ListBoxItem selected
                || selected.Tag is not string sessionId
                || string.IsNullOrWhiteSpace(sessionId))
            {
                AppendLog("Hãy chọn một Client trước khi gửi lệnh.");
                return;
            }

            if (CommandCombo.SelectedItem is not ComboBoxItem commandItem
                || commandItem.Tag is not string commandName
                || !Enum.TryParse(commandName, ignoreCase: true, out CommandType commandType))
            {
                AppendLog("Lệnh không hợp lệ.");
                return;
            }

            _isSending = true;
            SendCommandButton.IsEnabled = false;

            try
            {
                await _management.SendCommandAsync(sessionId, commandType);
            }
            catch (OperationCanceledException)
            {
                AppendLog("Đã hủy gửi lệnh.");
            }
            catch (Exception ex)
            {
                AppendLog($"Không gửi được lệnh: {ex.Message}");
            }
            finally
            {
                _isSending = false;

                if (!_isClosed)
                {
                    SendCommandButton.IsEnabled =
                        _serverRunning && !_isClosing;
                }
            }
        }
        private void UpdateClient(ConnectedClient client)
        {
            foreach (ListBoxItem item in ClientList.Items)
            {
                string? sessionId = item.Tag as string;

                if (sessionId == client.SessionId)
                {
                    item.Content = client.ToString();
                    break;
                }
            }
        }

        private void AppendLog(string line)
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}");
            LogBox.ScrollToEnd();
        }

        private static string ShortId(string sessionId)
        {
            return sessionId.Length <= 8 ? sessionId : sessionId[..8];
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _isClosed = true;

            _management.Stop();
        }
        private async void OnClosing( object? sender,System.ComponentModel.CancelEventArgs e)
        {
            if (_canClose)
            {
                return;
            }

            e.Cancel = true;

            if (_isClosing)
            {
                return;
            }

            _isClosing = true;
            _serverRunning = false;
            SendCommandButton.IsEnabled = false;
            StatusText.Text = "Đang dừng Server...";

            try
            {
                await _management.StopAsync();
            }
            catch (Exception ex)
            {
                AppendLog("Lỗi khi dừng: " + ex.Message);
            }
            finally
            {
                _canClose = true;
                Close();
            }
        }
    }
}
