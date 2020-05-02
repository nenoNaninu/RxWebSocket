using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace RxWebSocket
{
    public partial class WebSocketClient
    {
        public bool Send(string message)
        {
            return _webSocketMessageSender.Send(message);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        public bool Send(byte[] message)
        {
            return _webSocketMessageSender.Send(message);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        public bool Send(ArraySegment<byte> message)
        {
            return _webSocketMessageSender.Send(ref message);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        /// <param name="messageType"></param>
        public bool Send(byte[] message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSender.Send(message, messageType);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        /// <param name="messageType"></param>
        public bool Send(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSender.Send(ref message, messageType);
        }

        /// <summary>
        /// Send text message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(string message)
        {
            return _webSocketMessageSender.SendInstant(message);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(byte[] message)
        {
            return _webSocketMessageSender.SendInstant(message);
        }

        public Task SendInstant(byte[] message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSender.SendInstant(message, messageType);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(ArraySegment<byte> message)
        {
            return _webSocketMessageSender.SendInstant(ref message);
        }

        public Task SendInstant(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSender.SendInstant(ref message, messageType);
        }

        private void StartBackgroundThreadForSendingMessage()
        {
            _webSocketMessageSender.SendMessageFromQueue();
        }
    }
}