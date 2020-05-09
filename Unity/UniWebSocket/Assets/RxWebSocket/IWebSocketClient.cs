﻿using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using RxWebSocket.Exceptions;
using RxWebSocket.Message;

namespace RxWebSocket
{
    public interface IWebSocketClient : IDisposable
    {
        Uri Url { get; }

        IObservable<byte[]> BinaryMessageReceived { get; }

        IObservable<byte[]> RawTextMessageReceived { get; }

        IObservable<string> TextMessageReceived { get; }

        /// <summary>
        /// Invoke when a close message is received,
        /// before disconnecting the connection in normal system.
        /// </summary>
        IObservable<CloseMessage> CloseMessageReceived { get; }

        IObservable<WebSocketBackgroundException> ExceptionHappenedInBackground { get; }

        /// <summary>
        /// For logging purpose.
        /// </summary>
        string Name { get; }

        bool IsOpen { get; }

        bool IsClosed { get; }

        bool IsDisposed { get; }

        bool IsListening { get; }

        WebSocketState WebSocketState { get; }

        /// <summary>
        /// Returns currently used native websocket.
        /// </summary>
        WebSocket NativeSocket { get; }

        /// <summary>
        /// Default is UTF8
        /// </summary>
        Encoding MessageEncoding { get; }

        Task WaitUntilClose { get; }

        /// <summary>
        /// Start connect and listening to the websocket stream on the background thread
        /// </summary>
        Task ConnectAndStartListening();

        /// <summary>
        /// close websocket connection.
        /// </summary>
        Task CloseAsync(WebSocketCloseStatus status, string statusDescription, bool dispose);

        /// <summary>
        /// close websocket connection.
        /// </summary>
        Task CloseAsync(WebSocketCloseStatus status, string statusDescription);

        /// <summary>
        /// Send text message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        bool Send(string message);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        bool Send(byte[] message);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        bool Send(byte[] message, WebSocketMessageType messageType);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        bool Send(ArraySegment<byte> message);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        bool Send(ArraySegment<byte> message, WebSocketMessageType messageType);

        /// <summary>
        /// Send text message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        Task SendInstant(string message);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        Task SendInstant(byte[] message);

        /// <summary>
        /// Send message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        Task SendInstant(byte[] message, WebSocketMessageType messageType);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        Task SendInstant(ArraySegment<byte> message);

        /// <summary>
        /// Send message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        Task SendInstant(ArraySegment<byte> message, WebSocketMessageType messageType);
    }
}