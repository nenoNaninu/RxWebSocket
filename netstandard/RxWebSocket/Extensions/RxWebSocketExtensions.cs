using System;
using Microsoft.Extensions.Logging;

namespace RxWebSocket.Extensions
{
    public static class RxWebSocketExtensions
    {
        public static RxWebSocket.Logging.ILogger AsWebSocketLogger(this ILogger logger) => new Logger(logger);
        public static RxWebSocket.Logging.ILogger AsWebSocketLogger<T>(this ILogger<T> logger) => new Logger<T>(logger);
    }

    internal class Logger<T> : RxWebSocket.Logging.ILogger
    {
        private ILogger<T> _logger;

        public Logger(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void Log(string message)
        {
            _logger.Log(LogLevel.Information, message);
        }

        public void Error(string message)
        {
            _logger.LogError(message);
        }

        public void Error(Exception e, string message)
        {
            _logger.LogError(e, message);
        }

        public void Warn(string message)
        {
            _logger.LogWarning(message);
        }
    }
    
    internal class Logger : RxWebSocket.Logging.ILogger
    {
        private ILogger _logger;

        public Logger(ILogger logger)
        {
            _logger = logger;
        }

        public void Log(string message)
        {
            _logger.Log(LogLevel.Information, message);
        }

        public void Error(string message)
        {
            _logger.LogError(message);
        }

        public void Error(Exception e, string message)
        {
            _logger.LogError(e, message);
        }

        public void Warn(string message)
        {
            _logger.LogWarning(message);
        }
    }
}