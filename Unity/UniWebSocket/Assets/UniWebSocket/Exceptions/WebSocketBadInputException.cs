using System;

namespace UniWebSocket.Exceptions
{
    /// <summary>
    /// Custom exception that indicates bad user/client input
    /// </summary>
    public class WebSocketBadInputException : WebSocketException
    {
        /// <inheritdoc />
        public WebSocketBadInputException()
        {
        }

        /// <inheritdoc />
        public WebSocketBadInputException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public WebSocketBadInputException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
