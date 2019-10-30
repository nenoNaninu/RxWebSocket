using System.Net.WebSockets;

namespace WebSocketChat
{
    public class WebSocketWithName
    {
        public WebSocket Socket { get; }
        public string Name { get; }

        public WebSocketWithName(WebSocket websocket, string name)
        {
            Socket = websocket;
            Name = name;
        }
    }
}