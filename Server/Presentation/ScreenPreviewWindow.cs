using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Shared.Models;
namespace Server.Presentation
{
    public sealed class ScreenPreviewWindow : Window
    {
        private readonly Image _image;
        private readonly DispatcherTimer _mouseTimer;

        private (double X, double Y)? _pendingMousePosition;
        private readonly HashSet<MouseButtonType>  _pressedMouseButtons = new();

        private (double X, double Y) _lastMousePosition;

        public event Action<double, double, MouseButtonType, bool>?
            MouseButtonInputSelected;

        public event Action<double, double, int>?
            MouseWheelSelected;

        public event Action<double, double>? MousePositionSelected;
        public event Action<double, double, MouseButtonType>? MouseClickSelected;
        private readonly CheckBox _keyboardControl;

        private readonly HashSet<(int Code, bool Extended)>
            _pressedKeys = new();

        public event Action<int, bool, bool>? KeyInputSelected;

        public ScreenPreviewWindow(string sessionId)
        {
            Title = "Màn hình Client " + sessionId;
            Width = 1000;
            Height = 650;

            _image = new Image
            {
                Stretch = Stretch.Uniform
            };


            _image.MouseMove += OnImageMouseMove;

            _image.MouseDown += OnImageButtonDown;
            _image.MouseUp += OnImageButtonUp;
            _image.MouseWheel += OnImageMouseWheel;

            _image.LostMouseCapture += (_, _) =>
            {
                ReleaseMouseButtons();
            };

            _image.MouseLeave += (_, _) =>
            {
                if (!_image.IsMouseCaptured)
                {
                    _pendingMousePosition = null;
                }
            };
            Deactivated += (_, _) =>
            {
                ReleaseMouseButtons();
            };

            _mouseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            _mouseTimer.Tick += (_, _) =>
            {
                if (_pendingMousePosition is not { } position)
                {
                    return;
                }

                _pendingMousePosition = null;

                MousePositionSelected?.Invoke(
                    position.X,
                    position.Y);
            };

            _mouseTimer.Start();

            _image.Focusable = true;

            _keyboardControl = new CheckBox
            {
                Content = "Điều khiển bàn phím",
                Margin = new Thickness(8),
                Focusable = false
            };

            _keyboardControl.Checked += (_, _) =>
            {
                Keyboard.Focus(_image);
            };

            _keyboardControl.Unchecked += (_, _) =>
            {
                ReleasePressedKeys();
            };

            var layout = new DockPanel();

            DockPanel.SetDock(_keyboardControl, Dock.Top);

            layout.Children.Add(_keyboardControl);
            layout.Children.Add(_image);

            Content = layout;

            PreviewKeyDown += OnPreviewKeyDown;
            PreviewKeyUp += OnPreviewKeyUp;

            Deactivated += (_, _) =>
            {
                _keyboardControl.IsChecked = false;
            };

            Closed += (_, _) =>
            {
                _keyboardControl.IsChecked = false;
                ReleaseMouseButtons();

                _mouseTimer.Stop();
                _pendingMousePosition = null;
                _image.Source = null;
            };
        }

        private void OnImageMouseMove(object sender, MouseEventArgs e)
        {
            _pendingMousePosition = null;

            Point point = e.GetPosition(_image);

            if (!TryGetImagePosition(
                point,
                out double xRatio,
                out double yRatio))
            {
                return;
            }

            _pendingMousePosition = (xRatio, yRatio);
        }
        public void UpdateFrame(byte[] jpegBytes)
        {
            using var stream = new MemoryStream(jpegBytes);

            var bitmap = new BitmapImage();

            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 1600;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            _image.Source = bitmap;
        }
        private bool TryGetImagePosition(Point point,out double xRatio, out double yRatio, bool clampToImage = false)
        {
            xRatio = 0;
            yRatio = 0;

            if (_image.Source is not BitmapSource bitmap)
            {
                return false;
            }

            double areaWidth = _image.ActualWidth;
            double areaHeight = _image.ActualHeight;

            if (areaWidth <= 0 || areaHeight <= 0 ||
                bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                return false;
            }

            double scale = Math.Min(
                areaWidth / bitmap.Width,
                areaHeight / bitmap.Height);

            double displayedWidth = bitmap.Width * scale;
            double displayedHeight = bitmap.Height * scale;

            double offsetX = (areaWidth - displayedWidth) / 2;
            double offsetY = (areaHeight - displayedHeight) / 2;

            double imageX = point.X - offsetX;
            double imageY = point.Y - offsetY;

            if (!clampToImage && (imageX < 0 || imageX > displayedWidth || imageY < 0 || imageY > displayedHeight))
            {
                return false;
            }

            imageX = Math.Clamp(imageX, 0, displayedWidth);
            imageY = Math.Clamp(imageY, 0, displayedHeight);

            xRatio = imageX / displayedWidth;
            yRatio = imageY / displayedHeight;

            return true;
        }
        private static bool TryGetMouseButton( MouseButton button, out MouseButtonType result)
        {
            switch (button)
            {
                case MouseButton.Left:
                    result = MouseButtonType.Left;
                    return true;

                case MouseButton.Right:
                    result = MouseButtonType.Right;
                    return true;

                default:
                    result = default;
                    return false;
            }
        }
        private void OnImageMouseDown( object sender,MouseButtonEventArgs e)
        {
            MouseButtonType button;

            switch (e.ChangedButton)
            {
                case MouseButton.Left:
                    button = MouseButtonType.Left;
                    break;

                case MouseButton.Right:
                    button = MouseButtonType.Right;
                    break;

                default:
                    return;
            }

            Point point = e.GetPosition(_image);

            if (!TryGetImagePosition(
                point,
                out double xRatio,
                out double yRatio))
            {
                return;
            }

            // Không để timer gửi lại vị trí cũ đang chờ.
            _pendingMousePosition = null;

            e.Handled = true;

            MouseClickSelected?.Invoke(
                xRatio,
                yRatio,
                button);
        }
        private void OnImageButtonDown(
    object sender,
    MouseButtonEventArgs e)
        {
            if (!TryGetMouseButton(e.ChangedButton, out var button))
            {
                return;
            }

            if (!TryGetImagePosition(
                e.GetPosition(_image),
                out double xRatio,
                out double yRatio))
            {
                return;
            }

            if (!_image.IsMouseCaptured && !_image.CaptureMouse())
            {
                return;
            }

            if (!_pressedMouseButtons.Add(button))
            {
                return;
            }

            _lastMousePosition = (xRatio, yRatio);
            _pendingMousePosition = null;

            e.Handled = true;

            MouseButtonInputSelected?.Invoke(
                xRatio,
                yRatio,
                button,
                true);
        }
        private void OnImageButtonUp( object sender,MouseButtonEventArgs e)
        {
            if (!TryGetMouseButton(e.ChangedButton, out var button))
            {
                return;
            }

            if (!_pressedMouseButtons.Remove(button))
            {
                return;
            }

            if (TryGetImagePosition(
                e.GetPosition(_image),
                out double xRatio,
                out double yRatio,
                clampToImage: true))
            {
                _lastMousePosition = (xRatio, yRatio);
            }

            _pendingMousePosition = null;
            e.Handled = true;

            MouseButtonInputSelected?.Invoke(
                _lastMousePosition.X,
                _lastMousePosition.Y,
                button,
                false);

            if (_pressedMouseButtons.Count == 0 &&
                _image.IsMouseCaptured)
            {
                _image.ReleaseMouseCapture();
            }
        }
        private void OnImageMouseWheel(object sender,MouseWheelEventArgs e)
        {
            if (e.Delta == 0)
            {
                return;
            }

            if (!TryGetImagePosition(
                e.GetPosition(_image),
                out double xRatio,
                out double yRatio))
            {
                return;
            }

            _pendingMousePosition = null;
            e.Handled = true;

            MouseWheelSelected?.Invoke(
                xRatio,
                yRatio,
                e.Delta);
        }
        private void ReleaseMouseButtons()
        {
            var buttons = _pressedMouseButtons.ToArray();

            _pressedMouseButtons.Clear();
            _pendingMousePosition = null;

            if (buttons.Length == 0)
            {
                return;
            }

            if (_image.IsMouseCaptured)
            {
                _image.ReleaseMouseCapture();
            }

            foreach (var button in buttons)
            {
                MouseButtonInputSelected?.Invoke(
                    _lastMousePosition.X,
                    _lastMousePosition.Y,
                    button,
                    false);
            }
        }
        private static bool TryGetKeyInput(KeyEventArgs e,out int virtualKeyCode, out bool isExtended)
        {
            Key key = e.Key == Key.System
                ? e.SystemKey
                : e.Key;

            virtualKeyCode = KeyInterop.VirtualKeyFromKey(key);

            isExtended = key is
                Key.RightCtrl or
                Key.RightAlt or
                Key.Insert or
                Key.Delete or
                Key.Home or
                Key.End or
                Key.PageUp or
                Key.PageDown or
                Key.Left or
                Key.Right or
                Key.Up or
                Key.Down or
                Key.Divide or
                Key.NumLock or
                Key.PrintScreen or
                Key.LWin or
                Key.RWin or
                Key.Apps;

            return virtualKeyCode >= 1 &&
                   virtualKeyCode <= 254;
        }
        private void OnPreviewKeyDown( object sender, KeyEventArgs e)
        {
            if (_keyboardControl.IsChecked != true || !IsActive)
            {
                return;
            }

            if (!TryGetKeyInput(
                e,
                out int code,
                out bool extended))
            {
                return;
            }

            e.Handled = true;

            if (!_pressedKeys.Add((code, extended)))
            {
                return;
            }

            KeyInputSelected?.Invoke(code, extended, true);
        }
        private void OnPreviewKeyUp( object sender, KeyEventArgs e)
        {
            if (_keyboardControl.IsChecked != true)
            {
                return;
            }

            if (!TryGetKeyInput(
                e,
                out int code,
                out bool extended))
            {
                return;
            }

            e.Handled = true;

            if (!_pressedKeys.Remove((code, extended)))
            {
                return;
            }

            KeyInputSelected?.Invoke(code, extended, false);
        }
        private void ReleasePressedKeys()
        {
            var keys = _pressedKeys.ToArray();

            _pressedKeys.Clear();

            foreach (var key in keys)
            {
                KeyInputSelected?.Invoke(
                    key.Code,
                    key.Extended,
                    false);
            }
        }
    }
}