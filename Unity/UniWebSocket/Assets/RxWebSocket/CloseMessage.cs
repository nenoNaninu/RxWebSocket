using System.Net.WebSockets;

namespace RxWebSocket
{
    public readonly struct CloseMessage
    {
        public readonly string CloseStatusDescription;
        public readonly WebSocketCloseStatus CloseStatus;

        public CloseMessage(WebSocketCloseStatus closeStatus, string closeStatusDescription)
        {
            CloseStatus = closeStatus;
            CloseStatusDescription = closeStatusDescription;
        }
    }
}