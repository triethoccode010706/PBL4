using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Server.Presentation
{
    public sealed class ScreenPreviewWindow : Window
    {
        private readonly Image _image;

        public ScreenPreviewWindow(string sessionId)
        {
            Title = "Màn hình Client " + sessionId;
            Width = 1000;
            Height = 650;

            _image = new Image
            {
                Stretch = Stretch.Uniform
            };

            Content = _image;

            Closed += (_, _) =>
            {
                _image.Source = null;
            };
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
    }
}