using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace UniWebSocket
{
    /// <summary>
    /// A simple websocket client with built-in reconnection and error handling
    /// </summary>
    public interface IWebSocketClient : IDisposable
    {
        /// <summary>
        /// Get or set target websocket url
        /// </summary>
        Uri Url { get; set; }

        /// <summary>
        /// Stream with received message (raw format)
        /// </summary>
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
        IObservable<WebSocketErrorChunk> ErrorHappened { get; }

        /// <summary>
        /// Get or set the name of the current websocket client instance.
        /// For logging purpose (in case you use more parallel websocket clients and want to distinguish between them)
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Returns true if Start() method was called at least once. False if not started or disposed
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Returns true if client is running and connected to the server
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Returns currently used native websocket client.
        /// Use with caution, on every reconnection there will be a new instance. 
        /// </summary>
        ClientWebSocket NativeClient { get; }

        /// <summary>
        /// Sets used encoding for sending and receiving text messages.
        /// Default is UTF8
        /// </summary>
        Encoding MessageEncoding { get; set; }

        /// <summary>
        /// Start listening to the websocket stream on the background thread
        /// </summary>
        Task ConnectAndStartListening();

        /// <summary>
        /// Stop/close websocket connection with custom close code.
        /// Method could throw exceptions. 
        /// </summary>
        /// <returns>Returns true if close was initiated successfully</returns>
        Task<bool> CloseAsync(WebSocketCloseStatus status, string statusDescription, bool dispose = false);

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
        /// It doesn't use a sending queue, 
        /// beware of issue while sending two messages in the exact same time 
        /// on the full .NET Framework platform
        /// </summary>
        /// <param name="message">Message to be sent</param>
        Task SendInstant(string message);

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a sending queue, 
        /// beware of issue while sending two messages in the exact same time 
        /// on the full .NET Framework platform
        /// </summary>
        /// <param name="message">Message to be sent</param>
        Task SendInstant(byte[] message);
    }
}