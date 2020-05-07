using System;
using System.Threading.Channels;

namespace RxWebSocket.Senders
{
    public class DoubleQueueSender : WebSocketMessageSender
    {
        private readonly IWebSocketMessageSenderCore _core;

        public DoubleQueueSender(int binaryQueueCapacity, int textQueueCapacity)
        {
            Channel<ArraySegment<byte>> binaryMessageQueue = null;
            Channel<ArraySegment<byte>> textMessageQueue = null;

            if (0 < binaryQueueCapacity)
            {
                binaryMessageQueue = Channel.CreateBounded<ArraySegment<byte>>(new BoundedChannelOptions(binaryQueueCapacity) { SingleReader = true, SingleWriter = false });
            }

            if (0 < textQueueCapacity)
            {
                textMessageQueue = Channel.CreateBounded<ArraySegment<byte>>(new BoundedChannelOptions(textQueueCapacity) { SingleReader = true, SingleWriter = false });
            }

            _core = new DoubleQueueSenderCore(binaryMessageQueue, textMessageQueue);
        }

        public DoubleQueueSender(Channel<ArraySegment<byte>> binaryMessageQueue=null, Channel<ArraySegment<byte>> textMessageQueue=null)
        {
            _core = new DoubleQueueSenderCore(binaryMessageQueue, textMessageQueue);
        }

        internal override IWebSocketMessageSenderCore AsCore()
        {
            return _core;
        }
    }
}