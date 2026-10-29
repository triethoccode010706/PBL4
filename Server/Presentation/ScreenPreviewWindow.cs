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

        public event Action<double, double>? MousePositionSelected;
        public event Action<double, double, MouseButtonType>? MouseClickSelected;

        public ScreenPreviewWindow(string sessionId)
        {
            Title = "Màn hình Client " + sessionId;
            Width = 1000;
            Height = 650;

            _image = new Image
            {
                Stretch = Stretch.Uniform
            };

            // Tạm tắt di chuyển liên tục khi thử trên một máy.
            // _image.MouseMove += OnImageMouseMove;

            // Bật xử lý nhấn chuột trái và phải.
            _image.MouseDown += OnImageMouseDown;
            _image.MouseLeave += (_, _) =>
            {
                _pendingMousePosition = null;
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

            Content = _image;

            Closed += (_, _) =>
            {
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
        private bool TryGetImagePosition(
    Point point,
    out double xRatio,
    out double yRatio)
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

            if (imageX < 0 || imageX > displayedWidth ||
                imageY < 0 || imageY > displayedHeight)
            {
                return false;
            }

            xRatio = imageX / displayedWidth;
            yRatio = imageY / displayedHeight;

            return true;
        }
        private void OnImageMouseDown(
    object sender,
    MouseButtonEventArgs e)
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
    }
}