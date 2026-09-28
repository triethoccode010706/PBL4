using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Constants
{
    public static class CommandTypes
    {
        public const string PING = "PING";
        public const string GET_INFO = "GET_INFO";
        public const string START_SCREEN = "START_SCREEN";
        public const string STOP_SCREEN = "STOP_SCREEN";
        public const string MOUSE_MOVE = "MOUSE_MOVE";
        public const string MOUSE_CLICK = "MOUSE_CLICK";
        public const string KEY_DOWN = "KEY_DOWN";
        public const string KEY_UP = "KEY_UP";
        public const string DISCONNECT = "DISCONNECT";
    }
}
