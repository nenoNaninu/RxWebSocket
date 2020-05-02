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
        Encoding MessageEncoding { get; }

        string Name { get; set; }

        void SendMessageFromQueue();

        void SetInternal(
            CancellationToken sendingCancellationToken,
            CancellationToken waitQueueCancellationToken,
            ILogger logger);

        IObservable<WebSocketExceptionDetail> ExceptionHappenedInSending { get; }

        void SetSocket(WebSocket webSocket);

        bool Send(string message);

        bool Send(byte[] message);

        bool Send(byte[] message, WebSocketMessageType messageType);

        bool Send(ref ArraySegment<byte> message);

        bool Send(ref ArraySegment<byte> message, WebSocketMessageType messageType);

        Task SendInstant(string message);

        Task SendInstant(byte[] message);

        Task SendInstant(byte[] message, WebSocketMessageType messageType);

        Task SendInstant(ref ArraySegment<byte> message);

        Task SendInstant(ref ArraySegment<byte> message, WebSocketMessageType messageType);
    }
}