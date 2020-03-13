namespace RxWebSocket
{
    public enum ErrorType
    {
        Listen = 1,
        SendQueue = 1 << 1,
        Send = 1 << 2,
        CloseMessageReceive = 1 << 3
    }
}