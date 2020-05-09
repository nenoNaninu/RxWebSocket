using System;
using System.Net.WebSockets;

namespace RxWebSocket.Utils
{
    public static class WebSocketStatusExtensions
    {
        public static string ToStringFast(this WebSocketCloseStatus? webSocketCloseStatus)
        {
            switch (webSocketCloseStatus)
            {
                case null: return "null";
                case WebSocketCloseStatus.Empty: return "Empty";
                case WebSocketCloseStatus.EndpointUnavailable: return "EndpointUnavailable";
                case WebSocketCloseStatus.InternalServerError: return "InternalServerError";
                case WebSocketCloseStatus.InvalidMessageType: return "InvalidMessageType";
                case WebSocketCloseStatus.InvalidPayloadData: return "InvalidPayloadData";
                case WebSocketCloseStatus.MandatoryExtension: return "MandatoryExtension";
                case WebSocketCloseStatus.MessageTooBig: return "MessageTooBig";
                case WebSocketCloseStatus.NormalClosure: return "NormalClosure";
                case WebSocketCloseStatus.PolicyViolation: return "PolicyViolation";
                case WebSocketCloseStatus.ProtocolError: return "ProtocolError";
                default:
                    throw new ArgumentOutOfRangeException(nameof(webSocketCloseStatus), webSocketCloseStatus, null);
            }
        }
    }
}