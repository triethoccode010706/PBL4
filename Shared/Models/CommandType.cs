namespace Shared.Models
{
    public enum CommandType
    {
        PING = 1,
        GET_INFO = 2,
        START_SCREEN = 3,
        STOP_SCREEN = 4,
        MOUSE_MOVE = 5,
        MOUSE_CLICK = 6,
        KEY_DOWN = 7,
        KEY_UP = 8,
        DISCONNECT = 9
    }
}
