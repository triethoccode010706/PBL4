using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Client.Domain;
using Shared.Models;
using Shared.Protocol;

namespace Client.Application.Handlers
{
    public sealed class CaptureScreenCommandHandler : ICommandHandler
    {
        private readonly IScreenCapture _screenCapture;

        public CaptureScreenCommandHandler(
            IScreenCapture screenCapture)
        {
            _screenCapture = screenCapture;
        }

        public CommandType CommandType =>
            CommandType.CAPTURE_SCREEN;

        public Task<Response> HandleAsync(
            Command command,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            byte[] image = _screenCapture.CaptureJpeg(70);

            // Giới hạn cho bước thử truyền qua JSON.
            if (image.Length > 512 * 1024)
            {
                return Task.FromResult(
                    Response.Fail(
                        command.RequestId,
                        "IMAGE_TOO_LARGE",
                        "Ảnh vượt 512 KiB. Hãy giảm chất lượng JPEG."));
            }

            string base64 = Convert.ToBase64String(image);

            Response response = Response.Ok(
                command.RequestId,
                "Screenshot",
                base64);

            return Task.FromResult(response);
        }
    }
}