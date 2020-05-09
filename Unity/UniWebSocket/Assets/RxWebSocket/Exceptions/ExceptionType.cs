using System;

namespace RxWebSocket
{
    public enum ExceptionType
    {
        Listen = 1,
        SendQueue = 1 << 1,
        Send = 1 << 2,
        CloseMessageReceive = 1 << 3
    }

    public static class ExceptionTypeExtension
    {
        /// <summary>
        /// Overridden in extension methods.
        /// </summary>
        public static string ToStringFast(this ExceptionType exceptionType)
        {
            switch (exceptionType)
            {
                case ExceptionType.Listen: return "Listen";
                case ExceptionType.SendQueue: return "SendQueue";
                case ExceptionType.Send: return "Send";
                case ExceptionType.CloseMessageReceive: return "CloseMessageReceive";
                default: throw new ArgumentException(nameof(exceptionType));
            }
        }
    }
}