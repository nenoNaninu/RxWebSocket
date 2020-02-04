using System;
using Microsoft.Extensions.Logging;

namespace RxWebSocket.Logging
{
    public static class LogExtensions
    {
        public static ILogger AsWebSocketLogger(this Microsoft.Extensions.Logging.ILogger logger) => new Logger(logger);
    }

    internal class Logger : RxWebSocket.Logging.ILogger
    {
        private Microsoft.Extensions.Logging.ILogger _logger;

        public Logger(Microsoft.Extensions.Logging.ILogger logger)
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