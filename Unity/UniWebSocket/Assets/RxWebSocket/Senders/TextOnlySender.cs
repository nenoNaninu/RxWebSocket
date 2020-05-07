using System;
using System.Threading.Channels;

namespace RxWebSocket.Senders
{
    public class TextOnlySender: WebSocketMessageSender
    {
        private readonly IWebSocketMessageSenderCore _core;

        public TextOnlySender(int capacity)
        {
            if (0 < capacity)
            {
                var channel = Channel.CreateBounded<ArraySegment<byte>>(new BoundedChannelOptions(capacity) { SingleReader = true, SingleWriter = false });
                _core = new TextOnlySenderCore(channel);
            }
            else
            {
                _core = new TextOnlySenderCore();
            }
        }

        public TextOnlySender(Channel<ArraySegment<byte>> sentMessageQueue = null)
        {
            _core = new TextOnlySenderCore(sentMessageQueue);
        }
        internal override IWebSocketMessageSenderCore AsCore()
        {
            return _core;
        }
    }
}