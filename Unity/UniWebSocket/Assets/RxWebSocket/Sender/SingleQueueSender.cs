using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace RxWebSocket
{
    public class SingleQueueSender : IWebSocketMessageSender
    {
        public bool Send(string message)
        {
            throw new NotImplementedException();
        }

        public bool Send(byte[] message)
        {
            throw new NotImplementedException();
        }

        public bool Send(byte[] message, WebSocketMessageType messageType)
        {
            throw new NotImplementedException();
        }

        public bool Send(ArraySegment<byte> message)
        {
            throw new NotImplementedException();
        }

        public bool Send(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            throw new NotImplementedException();
        }

        public Task SendInstant(string message)
        {
            throw new NotImplementedException();
        }

        public Task SendInstant(byte[] message)
        {
            throw new NotImplementedException();
        }

        public Task SendInstant(byte[] message, WebSocketMessageType messageType)
        {
            throw new NotImplementedException();
        }

        public Task SendInstant(ArraySegment<byte> message)
        {
            throw new NotImplementedException();
        }

        public Task SendInstant(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            throw new NotImplementedException();
        }
    }
}