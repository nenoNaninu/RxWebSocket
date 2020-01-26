using System;

namespace UniWebSocket.Exceptions
{
    public class WebSocketBadInputException : WebSocketException
    {
        public WebSocketBadInputException()
        {
        }

        public WebSocketBadInputException(string message) : base(message)
        {
        }

        public WebSocketBadInputException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
