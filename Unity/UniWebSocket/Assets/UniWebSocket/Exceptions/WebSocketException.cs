using System;

namespace UniWebSocket.Exceptions
{
    /// <summary>
    /// Custom exception related to WebSocketClient
    /// </summary>
    public class WebSocketException : Exception
    {
        public WebSocketException()
        {
        }

        public WebSocketException(string message)
            : base(message)
        {
        }

        public WebSocketException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}