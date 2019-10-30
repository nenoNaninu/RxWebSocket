using System;

namespace UniWebSocket
{
    public class WebSocketErrorChunk
    {
        public Exception Exception { get; }
        public ErrorType ErrorType { get; }

        public WebSocketErrorChunk(Exception exception, ErrorType errorType)
        {
            Exception = exception;
            ErrorType = errorType;
        }
    }
}