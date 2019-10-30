namespace UniWebSocket
{
    public enum ErrorType
    {
        Start = 1,
        Close = 1 << 1,
        Dispose = 1 << 2,
        Listen = 1 << 3,
        SendBinary = 1 << 4,
        SendText = 1 << 5,
    }
}