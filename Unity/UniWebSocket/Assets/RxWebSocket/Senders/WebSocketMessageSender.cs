namespace RxWebSocket.Senders
{
    public abstract class WebSocketMessageSender
    {
        internal abstract IWebSocketMessageSenderCore AsCore();
    }
}