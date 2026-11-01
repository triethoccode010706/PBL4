using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using Client.Application;

namespace Client.Infrastructure
{
    public sealed class KeyboardController : IKeyboardController
    {
        private const uint InputKeyboard = 1;
        private const uint ExtendedKeyFlag = 0x0001;
        private const uint KeyUpFlag = 0x0002;

        private readonly object _sync = new();

        private readonly HashSet<(int Code, bool Extended)>
            _pressedKeys = new();

        public void KeyDown(int virtualKeyCode, bool isExtended)
        {
            ValidateKeyCode(virtualKeyCode);

            lock (_sync)
            {
                SendKey(virtualKeyCode, isExtended, isKeyUp: false);

                _pressedKeys.Add((virtualKeyCode, isExtended));
            }
        }

        public void KeyUp(int virtualKeyCode, bool isExtended)
        {
            ValidateKeyCode(virtualKeyCode);

            lock (_sync)
            {
                var key = (virtualKeyCode, isExtended);

                if (!_pressedKeys.Contains(key))
                {
                    return;
                }

                SendKey(virtualKeyCode, isExtended, isKeyUp: true);

                _pressedKeys.Remove(key);
            }
        }

        public void ReleaseAll()
        {
            lock (_sync)
            {
                var errors = new List<Exception>();

                foreach (var key in _pressedKeys.ToArray())
                {
                    try
                    {
                        SendKey(
                            key.Code,
                            key.Extended,
                            isKeyUp: true);

                        _pressedKeys.Remove(key);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }

                if (errors.Count > 0)
                {
                    throw new AggregateException(
                        "Không thể thả hết các phím đang giữ.",
                        errors);
                }
            }
        }

        private static void ValidateKeyCode(int virtualKeyCode)
        {
            if (virtualKeyCode < 1 || virtualKeyCode > 254)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(virtualKeyCode),
                    "Mã phím phải nằm trong khoảng 1 đến 254.");
            }
        }

        private static void SendKey(
            int virtualKeyCode,
            bool isExtended,
            bool isKeyUp)
        {
            uint flags = 0;

            if (isExtended)
            {
                flags |= ExtendedKeyFlag;
            }

            if (isKeyUp)
            {
                flags |= KeyUpFlag;
            }

            var input = new Input
            {
                Type = InputKeyboard,
                Data = new InputUnion
                {
                    Keyboard = new KeyboardInput
                    {
                        VirtualKey = (ushort)virtualKeyCode,
                        Flags = flags
                    }
                }
            };

            Input[] inputs = { input };

            uint sent = SendInput(
                (uint)inputs.Length,
                inputs,
                Marshal.SizeOf<Input>());

            if (sent != 1)
            {
                int errorCode = Marshal.GetLastWin32Error();

                throw new InvalidOperationException(
                    $"Không gửi được thao tác bàn phím. "
                    + $"Mã lỗi Windows: {errorCode}.");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            public uint Type;
            public InputUnion Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public KeyboardInput Keyboard;

            [FieldOffset(0)]
            public MouseInput Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyboardInput
        {
            public ushort VirtualKey;
            public ushort ScanCode;
            public uint Flags;
            public uint Time;
            public UIntPtr ExtraInfo;
        }

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(
            uint inputCount,
            [In] Input[] inputs,
            int inputSize);
    }
}