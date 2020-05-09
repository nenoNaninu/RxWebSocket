using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using RxWebSocket.Exceptions;
using RxWebSocket.Logging;

namespace RxWebSocket.Senders
{
    internal interface IWebSocketMessageSenderCore : IDisposable
    {
        string Name { get; }

        IObservable<WebSocketBackgroundException> ExceptionHappenedInSending { get; }

        void StartSendingMessageFromQueue();

        void SetConfig(Encoding encoding, ILogger logger, string name);

        void SetSocket(WebSocket webSocket);

        Task StopAsync();

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