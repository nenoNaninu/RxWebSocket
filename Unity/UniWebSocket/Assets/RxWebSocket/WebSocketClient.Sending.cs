using System;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RxWebSocket.Exceptions;
using RxWebSocket.Validations;

namespace RxWebSocket
{
    public partial class WebSocketClient
    {
        public bool Send(string message)
        {
            return _webSocketMessageSenderCore.Send(message);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        public bool Send(byte[] message)
        {
            return _webSocketMessageSenderCore.Send(message);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        public bool Send(ArraySegment<byte> message)
        {
            return _webSocketMessageSenderCore.Send(message);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        /// <param name="messageType"></param>
        public bool Send(byte[] message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSenderCore.Send(message, messageType);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        /// <param name="messageType"></param>
        public bool Send(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSenderCore.Send(message, messageType);
        }

        /// <summary>
        /// Send text message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(string message)
        {
            return _webSocketMessageSenderCore.SendInstant(message);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(byte[] message)
        {
            return _webSocketMessageSenderCore.SendInstant(message);
        }

        public Task SendInstant(byte[] message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSenderCore.SendInstant(message, messageType);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(ArraySegment<byte> message)
        {
            return _webSocketMessageSenderCore.SendInstant(message);
        }

        public Task SendInstant(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            return _webSocketMessageSenderCore.SendInstant(message, messageType);
        }

        private void StartBackgroundThreadForSendingMessage()
        {
#pragma warning disable 4014
            Task.Factory.StartNew(_ => _webSocketMessageSenderCore.SendMessageFromQueue(), TaskCreationOptions.LongRunning, _cancellationAllJobs.Token);
#pragma warning restore 4014
        }
    }
}