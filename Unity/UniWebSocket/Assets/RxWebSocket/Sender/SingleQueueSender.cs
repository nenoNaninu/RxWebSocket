using System.Threading.Channels;

namespace RxWebSocket
{
    public class SingleQueueSender : WebSocketMessageSender
    {
        private readonly IWebSocketMessageSender _core;

        public SingleQueueSender(Channel<SentMessage> sentMessageQueue = null)
        {
            _core = new SingleQueueSenderCore(sentMessageQueue);
        }

        internal override IWebSocketMessageSender AsCore()
        {
            return _core;
        }
    }
}