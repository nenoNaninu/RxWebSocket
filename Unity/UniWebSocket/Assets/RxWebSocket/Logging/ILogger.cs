using System;

namespace RxWebSocket.Logging
{
    public interface ILogger
    {
        void Log(string message);
        void Error(string message);
        void Error(Exception e, string message);
        void Warn(string message);
    }
}