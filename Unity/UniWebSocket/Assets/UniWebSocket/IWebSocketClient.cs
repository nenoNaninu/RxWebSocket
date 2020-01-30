using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace UniWebSocket
{
    public interface IWebSocketClient : IDisposable
    {
        /// <summary>
        /// target websocket url
        /// </summary>
        Uri Url { get; }

        IObservable<ResponseMessage> MessageReceived { get; }

        IObservable<byte[]> BinaryMessageReceived { get; }

        IObservable<string> TextMessageReceived { get; }

        /// <summary>
        /// Stream for disconnection event (triggered after the connection was lost) 
        /// </summary>
        IObservable<WebSocketCloseStatus> DisconnectionHappened { get; }

        /// <summary>
        /// Stream for exception event
        /// </summary>
        IObservable<WebSocketExceptionDetail> ErrorHappened { get; }

        /// <summary>
        /// Get or set the name of the current websocket client instance.
        /// For logging purpose (in case you use more parallel websocket clients and want to distinguish between them)
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Returns true if ConnectAndStartListening() method was called at least once. False if not started or disposed
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Returns true if client is running and connected to the server
        /// </summary>
        bool IsRunning { get; }

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
        /// Stop/close websocket connection with custom close code.
        /// </summary>
        /// <returns>Returns true if close was successfully</returns>
        Task<bool> CloseAsync(WebSocketCloseStatus status, string statusDescription, bool dispose);

        /// <summary>
        /// Send message to the websocket channel. 
        /// It inserts the message to the queue and actual sending is done on an other thread
        /// </summary>
        /// <param name="message">Message to be sent</param>
        void Send(string message);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It inserts the message to the queue and actual sending is done on an other thread
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        void Send(byte[] message);

        /// <summary>
        /// Send message to the websocket channel. 
        /// It doesn't use a sending queue
        /// </summary>
        /// <param name="message">Message to be sent</param>
        Task SendInstant(string message);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a sending queue, 
        /// </summary>
        /// <param name="message">Message to be sent</param>
        Task SendInstant(byte[] message);
    }
}