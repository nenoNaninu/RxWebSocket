using System;

namespace UniWebSocket
{
    public class WebSocketErrorDetail
    {
        public Exception Exception { get; }
        public ErrorType ErrorType { get; }

        public WebSocketErrorDetail(Exception exception, ErrorType errorType)
        {
            Exception = exception;
            ErrorType = errorType;
        }
    }
}