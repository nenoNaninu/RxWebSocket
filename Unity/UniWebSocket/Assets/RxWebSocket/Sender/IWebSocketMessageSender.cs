using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RxWebSocket.Logging;

namespace RxWebSocket
{
    internal interface IWebSocketMessageSender : IDisposable
    {
        /// <summary>
        /// Sets used encoding for sending and receiving text messages.
        /// Default is UTF8
        /// </summary>
        Encoding MessageEncoding { get; }

        void SendMessageFromQueue();

        void SetInternal(
            CancellationToken sendingCancellationToken,
            CancellationToken waitQueueCancellationToken,
            ILogger logger);

        IObservable<WebSocketExceptionDetail> ExceptionHappenedInSending { get; }

        void SetSocket(WebSocket webSocket);

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
        bool Send(ref ArraySegment<byte> message);

        bool Send(ref ArraySegment<byte> message, WebSocketMessageType messageType);

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
        Task SendInstant(ref ArraySegment<byte> message);

        Task SendInstant(ref ArraySegment<byte> message, WebSocketMessageType messageType);
    }
}