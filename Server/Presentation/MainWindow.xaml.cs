using System.Windows;
using System.Windows.Controls;
using Server.Application;
using Server.Domain;
using Shared.Models;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
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
        private readonly HashSet<string> _mouseMoveRequests = new();
        private readonly Dictionary<string, MouseMoveDto> _pendingMouseMoves = new();
        private readonly Dictionary<string, ScreenPreviewWindow> _screenWindows = new();

        private readonly ConcurrentDictionary<string, byte>  _pendingFrameUpdates = new();
        private readonly Dictionary<string, SemaphoreSlim> _inputLocks = new();
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
        private SemaphoreSlim GetInputLock(string sessionId)
        {
            if (!_inputLocks.TryGetValue(sessionId, out var inputLock))
            {
                inputLock = new SemaphoreSlim(1, 1);
                _inputLocks.Add(sessionId, inputLock);
            }

            return inputLock;
        }
        private void OpenScreenPreview(string sessionId)
        {
            if (_screenWindows.TryGetValue(
                sessionId,
                out var existingWindow))
            {
                existingWindow.Activate();
                return;
            }

            var window = new ScreenPreviewWindow(
                ShortId(sessionId));
            window.MouseClickSelected += async (xRatio, yRatio, button) =>
            {
                await SendMouseClickAsync(
                    sessionId,
                    xRatio,
                    yRatio,
                    button);
            };
            window.MousePositionSelected += async (xRatio, yRatio) =>
            {
                await SendMouseMoveAsync(sessionId, xRatio, yRatio);
            };

            window.Owner = this;

            window.Closed += (_, _) =>
            {
                _screenWindows.Remove(sessionId);
            };

            _screenWindows.Add(sessionId, window);

            window.Show();
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
                    AppendLog( $"← {ShortId(sessionId)}: {status} "  + $"{response.Message}");


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
            _management.ScreenFrameReceived += (sessionId, frame) =>
            {
                if (!_pendingFrameUpdates.TryAdd(sessionId, 0))
                {
                    // UI chưa xử lý xong ảnh trước:
                    // bỏ ảnh này, không xếp hàng thêm.
                    return;
                }

                RunOnUiThread(() =>
                {
                    try
                    {
                        if (_isClosing ||
                            _removedSessions.Contains(sessionId))
                        {
                            return;
                        }

                        if (_screenWindows.TryGetValue(
                            sessionId,
                            out var window))
                        {
                            window.UpdateFrame(frame.ImageData);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog(
                            "Không hiển thị được ảnh: " + ex.Message);
                    }
                    finally
                    {
                        _pendingFrameUpdates.TryRemove(
                            sessionId,
                            out _);
                    }
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
                Content = FormatClientInfo(client),
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
            if (_screenWindows.TryGetValue( sessionId,out var window))
            {
                window.Close();
            }
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
                Response response =await _management.SendCommandAsync(  sessionId, commandType);
                if (commandType == CommandType.START_SCREEN)
                {
                    if (response.Success &&
                        !_isClosing &&
                        !_isClosed &&
                        !_removedSessions.Contains(sessionId))
                    {
                        OpenScreenPreview(sessionId);
                    }
                }

                if (commandType == CommandType.CAPTURE_SCREEN)
                {
                    if (!response.Success)
                    {
                        AppendLog("Chụp màn hình thất bại: " + response.Message);
                        return;
                    }

                    if (!_isClosing && !_isClosed)
                    {
                        ShowScreenshot(sessionId, response.Data);
                    }
                }
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
                    item.Content = FormatClientInfo(client);
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
        private static string FormatClientInfo(ConnectedClient client)
        {
            ClientSystemInfo info = client.GetSystemInfoSnapshot();

            return $"Tên máy: {info.MachineName} | "
                + $"Người dùng: {info.UserName} | "
                + $"IP: {info.IpAddress} | "
                + $"Hệ điều hành: {info.OsDescription} | "
                + $"CPU: {info.CpuInfo} | "
                + $"RAM: {info.RamInfo}";
        }
        private void ShowScreenshot( string sessionId, string base64)
        {
            byte[] imageBytes = Convert.FromBase64String(base64);

            using var stream = new MemoryStream(imageBytes);

            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;

            // Giới hạn kích thước giải mã cho cửa sổ xem thử.
            bitmap.DecodePixelWidth = 1600;

            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            var imageControl = new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform
            };

            var preview = new Window
            {
                Title = "Màn hình Client " + ShortId(sessionId),
                Width = 1000,
                Height = 650,
                Owner = this,
                Content = imageControl
            };

            preview.Show();
        }
        private async Task SendMouseMoveAsync(string sessionId,double xRatio,  double yRatio)
        {
            if (!CanSendInput(sessionId))
            {
                return;
            }

            _pendingMouseMoves[sessionId] = new MouseMoveDto
            {
                XRatio = xRatio,
                YRatio = yRatio
            };

            if (!_mouseMoveRequests.Add(sessionId))
            {
                return;
            }

            SemaphoreSlim inputLock = GetInputLock(sessionId);

            try
            {
                while (CanSendInput(sessionId))
                {
                    await inputLock.WaitAsync();

                    try
                    {
                        if (!CanSendInput(sessionId))
                        {
                            break;
                        }

                        // Chỉ lấy vị trí sau khi đã có khóa.
                        if (!_pendingMouseMoves.Remove(
                            sessionId,
                            out MouseMoveDto? position))
                        {
                            break;
                        }

                        string json = JsonSerializer.Serialize(position);

                        Response response =
                            await _management.SendCommandAsync(
                                sessionId,
                                CommandType.MOUSE_MOVE,
                                json);

                        if (!response.Success)
                        {
                            if (!_isClosing && !_isClosed)
                            {
                                AppendLog(
                                    "Không di chuyển được chuột: "
                                    + response.Message);
                            }

                            break;
                        }
                    }
                    finally
                    {
                        inputLock.Release();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Phiên hoặc yêu cầu đã kết thúc.
            }
            catch (Exception ex)
            {
                if (!_isClosing && !_isClosed)
                {
                    AppendLog(
                        "Không gửi được lệnh di chuyển chuột: "
                        + ex.Message);
                }
            }
            finally
            {
                _pendingMouseMoves.Remove(sessionId);
                _mouseMoveRequests.Remove(sessionId);
            }
        }
        private bool CanSendInput(string sessionId)
        {
            return !_isClosing
                && !_isClosed
                && _serverRunning
                && !_removedSessions.Contains(sessionId);
        }
        private async Task SendMouseClickAsync( string sessionId,double xRatio,double yRatio, MouseButtonType button)
        {
            if (!CanSendInput(sessionId))
            {
                return;
            }

            // Bỏ vị trí di chuyển cũ chưa gửi.
            _pendingMouseMoves.Remove(sessionId);

            SemaphoreSlim inputLock = GetInputLock(sessionId);

            try
            {
                await inputLock.WaitAsync();

                try
                {
                    if (!CanSendInput(sessionId))
                    {
                        return;
                    }

                    // Bỏ các vị trí tích lũy trong lúc chờ khóa.
                    _pendingMouseMoves.Remove(sessionId);

                    var click = new MouseClickDto
                    {
                        XRatio = xRatio,
                        YRatio = yRatio,
                        Button = button
                    };

                    string json = JsonSerializer.Serialize(click);

                    Response response =
                        await _management.SendCommandAsync(
                            sessionId,
                            CommandType.MOUSE_CLICK,
                            json);

                    if (!response.Success && !_isClosing && !_isClosed)
                    {
                        AppendLog(
                            "Không click được chuột: "
                            + response.Message);
                    }
                }
                finally
                {
                    inputLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Phiên hoặc yêu cầu đã kết thúc.
            }
            catch (Exception ex)
            {
                if (!_isClosing && !_isClosed)
                {
                    AppendLog(
                        "Không gửi được lệnh click chuột: "
                        + ex.Message);
                }
            }
        }
    }
}
