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
        public void MoveTo(double xRatio, double yRatio)
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
        public void Click( double xRatio,double yRatio, MouseButtonType button)
        {
            uint downFlag;
            uint upFlag;

            switch (button)
            {
                case MouseButtonType.Left:
                    downFlag = LeftDown;
                    upFlag = LeftUp;
                    break;

                case MouseButtonType.Right:
                    downFlag = RightDown;
                    upFlag = RightUp;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(button),
                        "Nút chuột không được hỗ trợ.");
            }

            MoveTo(xRatio, yRatio);

            MouseInputPacket[] inputs =
            {
        CreateMouseInput(downFlag),
        CreateMouseInput(upFlag)
    };

            uint sent = SendInput(
                (uint)inputs.Length,
                inputs,
                Marshal.SizeOf<MouseInputPacket>());

            if (sent != (uint)inputs.Length)
            {
                int errorCode = Marshal.GetLastWin32Error();

                // Nếu chỉ gửi được nhấn, thử gửi thả để tránh giữ nút.
                if (sent == 1)
                {
                    MouseInputPacket[] release =
                    {
                CreateMouseInput(upFlag)
            };

                    SendInput(
                        (uint)release.Length,
                        release,
                        Marshal.SizeOf<MouseInputPacket>());
                }

                throw new InvalidOperationException(
                    $"Không gửi đủ thao tác click. "
                    + $"Đã gửi {sent}/{inputs.Length}; "
                    + $"mã lỗi Windows: {errorCode}.");
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
    }
}
