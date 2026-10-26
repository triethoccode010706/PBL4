using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Client.Domain;

namespace Client.Infrastructure
{
    public sealed class ScreenCaptureService : IScreenCapture
    {
        public byte[] CaptureJpeg(int quality = 40)
        {
            if (quality < 0 || quality > 100)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quality),
                    "Chất lượng JPEG phải từ 0 đến 100.");
            }

            Screen? screen = Screen.PrimaryScreen;

            if (screen == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy màn hình chính.");
            }

            Rectangle bounds = screen.Bounds;

            using var bitmap = new Bitmap(
                bounds.Width,
                bounds.Height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    bounds.Left,
                    bounds.Top,
                    0,
                    0,
                    bounds.Size);
            }

            ImageCodecInfo jpegCodec =
                ImageCodecInfo.GetImageEncoders()
                    .First(codec =>
                        codec.FormatID == ImageFormat.Jpeg.Guid);

            using var parameters = new EncoderParameters(1);

            parameters.Param[0] = new EncoderParameter(
                System.Drawing.Imaging.Encoder.Quality,
                (long)quality);

            using var stream = new MemoryStream();

            bitmap.Save(stream, jpegCodec, parameters);

            return stream.ToArray();
        }
    }
}