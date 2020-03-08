using System;
using System.Net.WebSockets;
using RxWebSocket.Exceptions;

namespace RxWebSocket
{
    public class SendMessage
    {
        public WebSocketMessageType MessageType { get; }
        public ArraySegment<byte> Bytes { get; }

        public SendMessage(ArraySegment<byte> bytes, WebSocketMessageType messageType)
        {
            if (messageType == WebSocketMessageType.Close)
            {
                throw new WebSocketBadInputException("Can not send close message type.");
            }
            Bytes = bytes;
            MessageType = messageType;
        }

        /// <summary>
        /// Return string info about the message
        /// </summary>
        public override string ToString()
        {
            if (MessageType == WebSocketMessageType.Binary)
            {
                return $"Type binary, length: {Bytes.Count}";
            }

            return $"Type Text, length: {Bytes.Count}";
        }
    }
}