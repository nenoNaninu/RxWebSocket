using System;
using System.Threading;
using UnityEngine;

namespace UniWebSocket
{
    public class UnityConsoleLogger : ILogger
    {
        private readonly SynchronizationContext _context;

        public UnityConsoleLogger()
        {
            _context = SynchronizationContext.Current;
        }

        public void Log(string message)
        {
            _context.Post(Debug.Log, message);
        }

        public void Error(string message)
        {
            _context.Post(Debug.LogError, message);
        }

        public void Error(Exception e, string message)
        {
            _context.Post(x =>
            {
                if (x is ErrorChunk errorChunk)
                {
                    Debug.LogError(errorChunk.Message);
                    Debug.LogException(errorChunk.Exception);
                }
            }, new ErrorChunk(message, e));
        }

        public void Warn(string message)
        {
            _context.Post(Debug.LogWarning, message);
        }

        private class ErrorChunk
        {
            public string Message { get; }
            public Exception Exception { get; }

            public ErrorChunk(string message, Exception e)
            {
                Message = message;
                Exception = e;
            }
        }
    }
}