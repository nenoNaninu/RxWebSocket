using System;

namespace UniWebSocket
{
    public class WebSocketExceptionDetail
    {
        public Exception Exception { get; }
        public ErrorType ErrorType { get; }

        public WebSocketExceptionDetail(Exception exception, ErrorType errorType)
        {
            Exception = exception;
            ErrorType = errorType;
        }
    }
}