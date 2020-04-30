using System.Threading.Channels;

namespace RxWebSocket
{
    public class SingleQueueSender : WebSocketMessageSender
    {
        private readonly IWebSocketMessageSenderCore _core;

        public SingleQueueSender(Channel<SentMessage> sentMessageQueue = null)
        {
            _core = new SingleQueueSenderCoreCore(sentMessageQueue);
        }

        internal override IWebSocketMessageSenderCore AsCore()
        {
            return _core;
        }
    }
}