using System;

namespace UniWebSocket.Exceptions
{
    /// <summary>
    /// Custom exception related to WebsocketClient
    /// </summary>
    public class WebSocketException : Exception
    {
        /// <inheritdoc />
        public WebSocketException()
        {
        }

        /// <inheritdoc />
        public WebSocketException(string message)
            : base(message)
        {
        }

        /// <inheritdoc />
        public WebSocketException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
