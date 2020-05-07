using System;

namespace RxWebSocket.Exceptions
{
    public class WebSocketBackgroundException : WebSocketException
    {
        public Exception Exception { get; }
        public ExceptionType ExceptionType { get; }

        public WebSocketBackgroundException(Exception exception, ExceptionType exceptionType) : base(exception.Message, exception)
        {
            Exception = exception;
            ExceptionType = exceptionType;
        }
    }
}