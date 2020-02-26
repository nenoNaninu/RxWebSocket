using System;
using UnityEngine;

namespace RxWebSocket.Logging
{
    public class UnityConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Debug.Log(message);
        }

        public void Error(string message)
        {
            Debug.LogError(message);
        }

        public void Error(Exception e, string message)
        {
            Debug.LogError(message);
            Debug.LogException(e);
        }

        public void Warn(string message)
        {
            Debug.LogWarning(message);
        }
    }
}