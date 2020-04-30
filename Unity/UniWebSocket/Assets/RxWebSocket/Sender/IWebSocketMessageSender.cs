using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace RxWebSocket
{
    public interface IWebSocketMessageSender
    {
        /// <summary>
        /// Send message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        bool Send(string message);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        bool Send(byte[] message);

        bool Send(byte[] message, WebSocketMessageType messageType);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        bool Send(ArraySegment<byte> message);

        bool Send(ArraySegment<byte> message, WebSocketMessageType messageType);

        /// <summary>
        /// Send message to the websocket channel. 
        /// It doesn't use a queue
        /// </summary>
        /// <param name="message">Message to be sent</param>
        Task SendInstant(string message);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue, 
        /// </summary>
        /// <param name="message">Message to be sent</param>
        Task SendInstant(byte[] message);

        Task SendInstant(byte[] message, WebSocketMessageType messageType);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue, 
        /// </summary>
        /// <param name="message">Message to be sent</param>
        Task SendInstant(ArraySegment<byte> message);

        Task SendInstant(ArraySegment<byte> message, WebSocketMessageType messageType);
    }
}