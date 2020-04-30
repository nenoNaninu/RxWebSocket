using System.Net.WebSockets;

namespace RxWebSocket
{
    /// <summary>
    /// Received message, could be Text or Binary
    /// </summary>
    public class ResponseMessage
    {
        private ResponseMessage(byte[] binary, WebSocketMessageType messageType)
        {
            Binary = binary;
            MessageType = messageType;
        }

        /// <summary>
        /// Received text message (only if type = WebSocketMessageType.Binary)
        /// </summary>
        public byte[] Binary { get; }

        /// <summary>
        /// Current message type (Text or Binary)
        /// </summary>
        public WebSocketMessageType MessageType { get; }

        /// <summary>
        /// Return string info about the message
        /// </summary>
        public override string ToString()
        {
            if (MessageType == WebSocketMessageType.Text)
            {
                return $"Type binary, length: {Binary?.Length}";
            }

            return $"Type binary, length: {Binary?.Length}";
        }

        /// <summary>
        /// Create text response message
        /// </summary>
        public static ResponseMessage TextMessage(byte[] data)
        {
            return new ResponseMessage(data, WebSocketMessageType.Text);
        }

        /// <summary>
        /// Create binary response message
        /// </summary>
        public static ResponseMessage BinaryMessage(byte[] data)
        {
            return new ResponseMessage(data, WebSocketMessageType.Binary);
        }
    }
}