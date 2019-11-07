using System;
using UnityEngine;

namespace UniWebSocket
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
        }

        public void Trace(string message)
        {
            Debug.Log(message);
        }
    }
}