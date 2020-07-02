using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace RxWebSocket
{
    public partial class WebSocketClient
    {
        ///<inheritdoc/>
        public bool Send(string message)
        {
            return _webSocketMessageSender.Send(message);
        }

        ///<inheritdoc/>
        public bool Send(byte[] message)
        {
            return _webSocketMessageSender.Send(message);
        }

        ///<inheritdoc/>
        public bool Send(ArraySegment<byte> message)
        {
            return _webSocketMessageSender.Send(ref message);
        }

        ///<inheritdoc/>
        public bool Send(byte[] message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSender.Send(message, messageType);
        }

        ///<inheritdoc/>
        public bool Send(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSender.Send(ref message, messageType);
        }

        ///<inheritdoc/>
        public Task SendInstant(string message)
        {
            return _webSocketMessageSender.SendInstant(message);
        }

        ///<inheritdoc/>
        public Task SendInstant(byte[] message)
        {
            return _webSocketMessageSender.SendInstant(message);
        }

        ///<inheritdoc/>
        public Task SendInstant(byte[] message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSender.SendInstant(message, messageType);
        }

        ///<inheritdoc/>
        public Task SendInstant(ArraySegment<byte> message)
        {
            return _webSocketMessageSender.SendInstant(ref message);
        }

        ///<inheritdoc/>
        public Task SendInstant(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSender.SendInstant(ref message, messageType);
        }
    }
}