using Server.Application;
using Server.Domain;
using Shared.Models;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
      
        
        private readonly Dictionary<string, ScreenPreviewWindow> _screenWindows = new();

        private readonly ConcurrentDictionary<string, byte>  _pendingFrameUpdates = new();
        private readonly Dictionary< string,LinkedList<(CommandType Type, string Data)>> _inputQueues = new();

        private readonly HashSet<string> _inputWorkers = new();

        private readonly HashSet<string> _inputFailedSessions = new();
        private readonly HashSet<string> _stoppingScreens = new();

        private readonly Dictionary<string, Task> _inputTasks = new();


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
            window.StopRequested += async () =>
            {
                await StopScreenAsync(sessionId);
            };
            window.KeyInputSelected += (code, extended, isDown) =>
            {
                EnqueueKeyInput(
                    sessionId,
                    code,
                    extended,
                    isDown);
            };
            window.MousePositionSelected += (xRatio, yRatio) =>
            {
                var input = new MouseMoveDto
                {
                    XRatio = xRatio,
                    YRatio = yRatio
                };

                EnqueueInput(
                    sessionId,
                    CommandType.MOUSE_MOVE,
                    JsonSerializer.Serialize(input));
            };
            window.MouseButtonInputSelected +=
    (xRatio, yRatio, button, isDown) =>
    {
        var input = new MouseClickDto
        {
            XRatio = xRatio,
            YRatio = yRatio,
            Button = button
        };

        CommandType commandType = isDown
            ? CommandType.MOUSE_DOWN
            : CommandType.MOUSE_UP;

        EnqueueInput(
            sessionId,
            commandType,
            JsonSerializer.Serialize(input));
    };
            window.MouseWheelSelected += (xRatio, yRatio, delta) =>
            {
                var input = new MouseWheelDto
                {
                    XRatio = xRatio,
                    YRatio = yRatio,
                    Delta = delta
                };

                EnqueueInput(
                    sessionId,
                    CommandType.MOUSE_WHEEL,
                    JsonSerializer.Serialize(input));
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
                window.CloseAfterStop();
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
            if (_stoppingScreens.Contains(sessionId))
            {
                AppendLog("Đang dừng màn hình của Client này.");
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
                if (commandType == CommandType.STOP_SCREEN)
                {
                    await StopScreenAsync(sessionId);
                    return;
                }
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
            foreach (var window in _screenWindows.Values.ToArray())
            {
                window.CloseAfterStop();
                foreach (var previewWindow in _screenWindows.Values.ToArray())
                {
                    previewWindow.CloseAfterStop();
                }
            }

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

        private bool CanSendInput(string sessionId)
        {
            return !_isClosing
                && !_isClosed
                && _serverRunning
                && !_removedSessions.Contains(sessionId)
                && !_stoppingScreens.Contains(sessionId);
        }
        void EnqueueKeyInput(
    string sessionId,
    int code,
    bool extended,
    bool isDown)
        {
            var input = new KeyInputDto
            {
                VirtualKeyCode = code,
                IsExtended = extended
            };

            CommandType commandType = isDown
                ? CommandType.KEY_DOWN
                : CommandType.KEY_UP;

            EnqueueInput(
                sessionId,
                commandType,
                JsonSerializer.Serialize(input));
        }
        private async Task ProcessInputQueueAsync(string sessionId)
        {
            try
            {
                while (CanSendInput(sessionId) &&
                       _inputQueues.TryGetValue(sessionId, out var queue) &&
                       queue.First != null)
                {
                    var item = queue.First.Value;
                    queue.RemoveFirst();

                    Response response =
                        await _management.SendCommandAsync(
                            sessionId,
                            item.Type,
                            item.Data);

                    if (!response.Success)
                    {
                        throw new InvalidOperationException(
                            response.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _inputFailedSessions.Add(sessionId);

                if (!_isClosing && !_isClosed)
                {
                    AppendLog(
                        "Dừng điều khiển phiên vì lỗi: "
                        + ex.Message);
                }

                await TryDisconnectAfterInputErrorAsync(sessionId);
            }
            finally
            {
                _inputQueues.Remove(sessionId);
                _inputWorkers.Remove(sessionId);
                _inputTasks.Remove(sessionId);
            }
        }
        private async Task TryDisconnectAfterInputErrorAsync(
    string sessionId)
        {
            if (!CanSendInput(sessionId))
            {
                return;
            }

            try
            {
                using var deadline = new CancellationTokenSource(
                    TimeSpan.FromSeconds(3));

                Response response =
                    await _management.SendCommandAsync(
                        sessionId,
                        CommandType.DISCONNECT,
                        deadline.Token);

                if (!response.Success && !_isClosing && !_isClosed)
                {
                    AppendLog(
                        "Client chưa xác nhận ngắt phiên: "
                        + response.Message);
                }
            }
            catch (Exception ex)
            {
                if (!_isClosing && !_isClosed)
                {
                    AppendLog(
                        "Không gửi được yêu cầu ngắt phiên: "
                        + ex.Message);
                }
            }
        }
       
        private void EnqueueInput(
    string sessionId,
    CommandType commandType,
    string data)
        {
            if (!CanSendInput(sessionId) ||
                _inputFailedSessions.Contains(sessionId))
            {
                return;
            }

            if (!_inputQueues.TryGetValue(sessionId, out var queue))
            {
                queue = new LinkedList<(CommandType Type, string Data)>();
                _inputQueues.Add(sessionId, queue);
            }

            var last = queue.Last;

            if (commandType == CommandType.MOUSE_MOVE &&
                last != null &&
                last.Value.Type == CommandType.MOUSE_MOVE)
            {
                last.Value = (commandType, data);
            }
            else
            {
                queue.AddLast((commandType, data));
            }

            if (_inputWorkers.Add(sessionId))
            {
                Task task = ProcessInputQueueAsync(sessionId);

                if (!task.IsCompleted)
                {
                    _inputTasks[sessionId] = task;
                }
            }
        }
        private async Task StopScreenAsync(string sessionId)
        {
            if (_isClosing || _isClosed || !_serverRunning ||
                _removedSessions.Contains(sessionId))
            {
                if (_screenWindows.TryGetValue(sessionId, out var closedWindow))
                {
                    closedWindow.CloseAfterStop();
                }

                return;
            }

            if (!_stoppingScreens.Add(sessionId))
            {
                return;
            }

            try
            {
                if (_screenWindows.TryGetValue(sessionId, out var window))
                {
                    window.BeginStopping();
                }

                if (_inputQueues.TryGetValue(sessionId, out var queue))
                {
                    queue.Clear();
                }

                if (_inputTasks.TryGetValue(sessionId, out Task? inputTask))
                {
                    await inputTask;
                }

                if (_removedSessions.Contains(sessionId))
                {
                    return;
                }

                using var deadline = new CancellationTokenSource(
                    TimeSpan.FromSeconds(5));

                Response response = await _management.SendCommandAsync(
                    sessionId,
                    CommandType.STOP_SCREEN,
                    deadline.Token);

                if (!response.Success)
                {
                    throw new InvalidOperationException(response.Message);
                }

                // Client đã xác nhận giải phóng trạng thái input.
                _inputFailedSessions.Remove(sessionId);

                if (_screenWindows.TryGetValue(sessionId, out var preview))
                {
                    preview.CloseAfterStop();
                }
            }
            catch (Exception ex)
            {
                if (!_isClosing && !_isClosed)
                {
                    AppendLog(
                        "Không hoàn tất được việc dừng màn hình: "
                        + ex.Message);
                }

                if (_screenWindows.TryGetValue(sessionId, out var window))
                {
                    window.AllowStopRetry();
                }
            }
            finally
            {
                _stoppingScreens.Remove(sessionId);
            }
        }

    }
}
