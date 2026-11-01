using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Client.Application;
using Shared.Models;
namespace Client.Infrastructure
{
    public sealed class MouseController : IMouseController
    {
        private const uint InputMouse = 0;

        private const uint LeftDown = 0x0002;
        private const uint LeftUp = 0x0004;

        private const uint RightDown = 0x0008;
        private const uint RightUp = 0x0010;
        private readonly object _sync = new();

        private readonly HashSet<MouseButtonType> _pressedButtons = new();

        private const uint WheelFlag = 0x0800;

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInput
        {
            public int Dx;
            public int Dy;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

        // Biểu diễn INPUT cho trường hợp chỉ gửi thao tác chuột.
        [StructLayout(LayoutKind.Sequential)]
        private struct MouseInputPacket
        {
            public uint Type;
            public MouseInput Mouse;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(
            uint inputCount,
            [In] MouseInputPacket[] inputs,
            int inputSize);
        private void MoveToCore(double xRatio, double yRatio)
        {
            if (!double.IsFinite(xRatio) ||
                xRatio < 0 || xRatio > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(xRatio),
                    "Tọa độ X phải từ 0 đến 1.");
            }

            if (!double.IsFinite(yRatio) ||
                yRatio < 0 || yRatio > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(yRatio),
                    "Tọa độ Y phải từ 0 đến 1.");
            }

            Screen screen = Screen.PrimaryScreen
                ?? throw new InvalidOperationException(
                    "Không tìm thấy màn hình chính.");

            var bounds = screen.Bounds;

            int x = bounds.Left
                + (int)Math.Round(xRatio * (bounds.Width - 1));

            int y = bounds.Top
                + (int)Math.Round(yRatio * (bounds.Height - 1));

            if (!SetCursorPos(x, y))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }
        }
        public void MoveTo(double xRatio, double yRatio)
        {
            lock (_sync)
            {
                MoveToCore(xRatio, yRatio);
            }
        }
        public void Click(double xRatio,double yRatio, MouseButtonType button)
        {
            lock (_sync)
            {
                if (_pressedButtons.Contains(button))
                {
                    throw new InvalidOperationException(
                        "Nút chuột này đang được giữ.");
                }

                ButtonDown(xRatio, yRatio, button);
                ButtonUp(xRatio, yRatio, button);
            }
        }
        private static (uint Down, uint Up) GetButtonFlags(MouseButtonType button)
        {
            return button switch
            {
                MouseButtonType.Left => (LeftDown, LeftUp),
                MouseButtonType.Right => (RightDown, RightUp),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(button),
                    "Nút chuột không được hỗ trợ.")
            };
        }
        public void ButtonDown(double xRatio, double yRatio, MouseButtonType button)
        {
            var flags = GetButtonFlags(button);

            lock (_sync)
            {
                MoveToCore(xRatio, yRatio);

                if (_pressedButtons.Contains(button))
                {
                    return;
                }

                SendMouseEvent(flags.Down);

                _pressedButtons.Add(button);
            }
        }

        public void ButtonUp(double xRatio,  double yRatio,MouseButtonType button)
        {
            var flags = GetButtonFlags(button);

            lock (_sync)
            {
                if (!_pressedButtons.Contains(button))
                {
                    return;
                }

                try
                {
                    MoveToCore(xRatio, yRatio);
                }
                finally
                {
                    SendMouseEvent(flags.Up);

                    _pressedButtons.Remove(button);
                }
            }
        }

        public void Scroll(double xRatio,double yRatio, int delta)
        {
            if (delta == 0)
            {
                return;
            }

            lock (_sync)
            {
                MoveToCore(xRatio, yRatio);

                SendMouseEvent(
                    WheelFlag,
                    unchecked((uint)delta));
            }
        }

        public void ReleaseAll()
        {
            lock (_sync)
            {
                var errors = new List<Exception>();

                foreach (var button in _pressedButtons.ToArray())
                {
                    try
                    {
                        var flags = GetButtonFlags(button);

                        SendMouseEvent(flags.Up);

                        _pressedButtons.Remove(button);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }

                if (errors.Count > 0)
                {
                    throw new AggregateException(
                        "Không thể thả hết các nút chuột đang giữ.",
                        errors);
                }
            }
        }
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);
        private static MouseInputPacket CreateMouseInput(uint flags)
        {
            return new MouseInputPacket
            {
                Type = InputMouse,
                Mouse = new MouseInput
                {
                    Flags = flags
                }
            };
        }
        private static void SendMouseEvent( uint flags, uint mouseData = 0)
        {
            MouseInputPacket[] inputs =
            {
        new MouseInputPacket{ Type = InputMouse,Mouse = new MouseInput
            {
                Flags = flags,
                MouseData = mouseData
            }
        }
    };

            uint sent = SendInput(
                (uint)inputs.Length,
                inputs,
                Marshal.SizeOf<MouseInputPacket>());

            if (sent != 1)
            {
                int errorCode = Marshal.GetLastWin32Error();

                throw new InvalidOperationException(
                    $"Không gửi được thao tác chuột. "
                    + $"Mã lỗi Windows: {errorCode}.");
            }
        }
    }
}
