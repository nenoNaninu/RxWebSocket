using System;
using System.Threading.Channels;

namespace RxWebSocket
{
    public class DoubleQueueSender : WebSocketMessageSender
    {
        private readonly IWebSocketMessageSender _core;

        public DoubleQueueSender(Channel<ArraySegment<byte>> binaryMessageQueue=null, Channel<ArraySegment<byte>> textMessageQueue=null)
        {
            _core = new DoubleQueueSenderCore(binaryMessageQueue, textMessageQueue);
        }

        internal override IWebSocketMessageSender AsCore()
        {
            return _core;
        }
    }
}