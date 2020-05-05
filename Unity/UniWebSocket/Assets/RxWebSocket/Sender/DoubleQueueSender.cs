using System;
using System.Threading.Channels;

namespace RxWebSocket
{
    public class DoubleQueueSender : WebSocketMessageSender
    {
        private readonly IWebSocketMessageSenderCore _core;

        public DoubleQueueSender(int binaryChannelCapacity, int textChannelCapacity)
        {
            Channel<ArraySegment<byte>> binaryMessageQueue = null;
            Channel<ArraySegment<byte>> textMessageQueue = null;

            if (0 < binaryChannelCapacity)
            {
                binaryMessageQueue = Channel.CreateBounded<ArraySegment<byte>>(new BoundedChannelOptions(binaryChannelCapacity) { SingleReader = true, SingleWriter = false });
            }

            if (0 < textChannelCapacity)
            {
                textMessageQueue = Channel.CreateBounded<ArraySegment<byte>>(new BoundedChannelOptions(textChannelCapacity) { SingleReader = true, SingleWriter = false });
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