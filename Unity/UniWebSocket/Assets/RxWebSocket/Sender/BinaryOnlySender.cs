using System;
using System.Threading.Channels;

namespace RxWebSocket
{
    public class BinaryOnlySender : WebSocketMessageSender
    {
        private readonly IWebSocketMessageSenderCore _core;

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