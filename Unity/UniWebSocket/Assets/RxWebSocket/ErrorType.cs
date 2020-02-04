namespace RxWebSocket
{
    public enum ErrorType
    {
        Start = 1,
        Close = 1 << 1,
        Dispose = 1 << 2,
        Listen = 1 << 3,
        SendBinary = 1 << 4,
        SendText = 1 << 5,
        BinaryQueue = 1 << 6,
        TextQueue = 1 << 7,
    }
}