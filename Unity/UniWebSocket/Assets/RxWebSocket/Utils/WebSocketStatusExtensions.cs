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

        public static string ToStringFast(this WebSocketState webSocketState)
        {
            switch (webSocketState)
            {
                case WebSocketState.Aborted: return "Aborted";
                case WebSocketState.Closed: return "Closed";
                case WebSocketState.CloseReceived: return "CloseReceived";
                case WebSocketState.CloseSent: return "CloseSent";
                case WebSocketState.Connecting: return "Connecting";
                case WebSocketState.None: return "None";
                case WebSocketState.Open: return "Open";
                default:
                    throw new ArgumentOutOfRangeException(nameof(webSocketState), webSocketState, null);
            }
        }
    }
}