using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace RxWebSocket
{
    public interface IWebSocketClient : IDisposable
    {
        /// <summary>
        /// Target websocket url
        /// </summary>
        Uri Url { get; }

        IObservable<ResponseMessage> MessageReceived { get; }

        IObservable<byte[]> BinaryMessageReceived { get; }

        IObservable<string> TextMessageReceived { get; }

        /// <summary>
        /// Triggered after the connection was lost.
        /// </summary>
        IObservable<WebSocketCloseStatus> CloseMessageReceived { get; }
        
        IObservable<WebSocketExceptionDetail> ExceptionHappened { get; }

        /// <summary>
        /// For logging purpose.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Returns true if ConnectAndStartListening() method was already called.
        /// Returns False if ConnectAndStartListening is not called or already call Dispose().
        /// </summary>
        bool IsStarted { get; }

        bool IsOpen { get; }

        bool IsClosed { get; }

        /// <summary>
        /// Returns true if the client is already disposed.
        /// </summary>
        bool IsDisposed { get; }

        WebSocketState WebSocketState { get; }

        /// <summary>
        /// Returns currently used native websocket client.
        /// </summary>
        ClientWebSocket NativeClient { get; }

        /// <summary>
        /// Returns currently used native websocket.
        /// </summary>
        WebSocket NativeSocket { get; }

        /// <summary>
        /// Sets used encoding for sending and receiving text messages.
        /// Default is UTF8
        /// </summary>
        Encoding MessageEncoding { get; set; }

        /// <summary>
        /// Start connect and listening to the websocket stream on the background thread
        /// </summary>
        Task<bool> ConnectAndStartListening();

        /// <summary>
        /// close websocket connection.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="statusDescription"></param>
        /// <param name="dispose"></param>
        /// <returns>Returns true if close was successful</returns>
        Task<bool> CloseAsync(WebSocketCloseStatus status, string statusDescription, bool dispose);
        
        /// <summary>
        /// close websocket connection.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="statusDescription"></param>
        /// <returns>Returns true if close was successful</returns>
        Task<bool> CloseAsync(WebSocketCloseStatus status, string statusDescription);

        /// <summary>
        /// Send message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        void Send(string message);
        
        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        void Send(byte[] message);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        void Send(ArraySegment<byte> message);

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

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue, 
        /// </summary>
        /// <param name="message">Message to be sent</param>
        Task SendInstant(ArraySegment<byte> message);
    }
}