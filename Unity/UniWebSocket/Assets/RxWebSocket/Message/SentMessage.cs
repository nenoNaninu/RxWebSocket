using System;
using System.Net.WebSockets;
using RxWebSocket.Exceptions;

namespace RxWebSocket.Message
{
    public class SentMessage
    {
        public WebSocketMessageType MessageType { get; }
        public ArraySegment<byte> Bytes { get; }

        public SentMessage(ArraySegment<byte> bytes, WebSocketMessageType messageType)
        {
            if (messageType == WebSocketMessageType.Close)
            {
                throw new WebSocketBadInputException("Can not send close message type.");
            }
            Bytes = bytes;
            MessageType = messageType;
        }

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