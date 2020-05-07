using System;
using System.Threading.Channels;

namespace RxWebSocket.Senders
{
    public class BinaryOnlySender : WebSocketMessageSender
    {
        private readonly IWebSocketMessageSenderCore _core;

        public BinaryOnlySender(int capacity)
        {
            if (0 < capacity)
            {
                var channel = Channel.CreateBounded<ArraySegment<byte>>(new BoundedChannelOptions(capacity) { SingleReader = true, SingleWriter = false });
                _core = new BinaryOnlySenderCore(channel);
            }
            else
            {
                _core = new BinaryOnlySenderCore();
            }
        }

        public BinaryOnlySender(Channel<ArraySegment<byte>> sentMessageQueue = null)
        {
            _core = new BinaryOnlySenderCore(sentMessageQueue);
        }

        internal override IWebSocketMessageSenderCore AsCore()
        {
            return _core;
        }
    }
}