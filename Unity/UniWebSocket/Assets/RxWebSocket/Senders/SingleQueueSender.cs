using System.Threading.Channels;
using RxWebSocket.Message;

namespace RxWebSocket.Senders
{
    public class SingleQueueSender : WebSocketMessageSender
    {
        private readonly IWebSocketMessageSenderCore _core;

        public SingleQueueSender(int capacity)
        {
            if (0 <= capacity)
            {
                var channel = Channel.CreateBounded<SentMessage>(new BoundedChannelOptions(capacity) { SingleReader = true, SingleWriter = false });
                _core = new SingleQueueSenderCore(channel);
            }
            else
            {
                _core = new SingleQueueSenderCore();
            }
        }

        public SingleQueueSender(Channel<SentMessage> sentMessageQueue = null)
        {
            _core = new SingleQueueSenderCore(sentMessageQueue);
        }

        internal override IWebSocketMessageSenderCore AsCore()
        {
            return _core;
        }
    }
}