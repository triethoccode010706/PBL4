using Shared.Models;
using Shared.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Domain;
namespace Client.Application
{
    public sealed class ScreenStreamingService : IScreenStreamingService
    {
        private readonly IScreenCapture _capture;
        private readonly IScreenFrameSender _frameSender;

        private readonly SemaphoreSlim _lifecycleLock = new(1, 1);

        private CancellationTokenSource? _streamCts;
        private Task? _streamTask;

        public ScreenStreamingService( IScreenCapture capture, IScreenFrameSender frameSender)
        {
            _capture = capture;
            _frameSender = frameSender;
        }

        public async Task StartAsync(CancellationToken sessionToken)
        {
            await _lifecycleLock.WaitAsync(sessionToken);

            try
            {
                // Đang chạy thì không tạo thêm vòng lặp.
                if (_streamTask != null &&
                    !_streamTask.IsCompleted)
                {
                    return;
                }

                _streamCts?.Dispose();

                _streamCts =
                    CancellationTokenSource.CreateLinkedTokenSource(
                        sessionToken);

                CancellationToken streamToken = _streamCts.Token;

                _streamTask = Task.Run( () => StreamAsync(streamToken, sessionToken));
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        public async Task StopAsync()
        {
            await _lifecycleLock.WaitAsync();

            try
            {
                if (_streamCts == null)
                {
                    return;
                }

                _streamCts.Cancel();

                if (_streamTask != null)
                {
                    await _streamTask;
                }

                _streamCts.Dispose();

                _streamCts = null;
                _streamTask = null;
            }
            finally
            {
                _lifecycleLock.Release();
            }
        }

        private async Task StreamAsync(CancellationToken cancellationToken,CancellationToken sessionToken)
        {
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    byte[] image = _capture.CaptureJpeg(40);

                    cancellationToken.ThrowIfCancellationRequested();

                    if (image.Length > 512 * 1024)
                    {
                        throw new InvalidOperationException(
                            "Ảnh vượt 512 KiB. Cần giảm kích thước "
                            + "hoặc chất lượng trước khi streaming.");
                    }

                    var frame = new ScreenFrame
                    {
                        ImageData = image
                    };

                    // Giới hạn thời gian chờ gửi một ảnh.
                    using var sendDeadline =CancellationTokenSource.CreateLinkedTokenSource(sessionToken);

                    sendDeadline.CancelAfter(
                        TimeSpan.FromSeconds(3));

                    await _frameSender.SendAsync(frame, sendDeadline.Token);

                    await Task.Delay(
                        500,
                        cancellationToken);
                }
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // STOP_SCREEN hoặc phiên đang kết thúc.
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "Streaming đã dừng do lỗi: " + ex.Message);
            }
        }
    }
}