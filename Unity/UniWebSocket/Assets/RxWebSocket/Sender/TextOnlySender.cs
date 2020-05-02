using System;
using System.Threading.Channels;

namespace RxWebSocket
{
    public class TextOnlySender: WebSocketMessageSender
    {
        private readonly IWebSocketMessageSenderCore _core;

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