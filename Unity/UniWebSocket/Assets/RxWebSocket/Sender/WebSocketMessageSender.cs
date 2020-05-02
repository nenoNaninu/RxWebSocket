namespace RxWebSocket
{
    public abstract class WebSocketMessageSender
    {
        internal abstract IWebSocketMessageSenderCore AsCore();
    }
}