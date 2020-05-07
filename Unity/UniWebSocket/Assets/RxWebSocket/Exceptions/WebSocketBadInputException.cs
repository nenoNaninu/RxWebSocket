using System;

namespace RxWebSocket.Exceptions
{
    public class WebSocketBadInputException : WebSocketException
    {
        public WebSocketBadInputException(string message) : base(message)
        {
        }

        public WebSocketBadInputException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}