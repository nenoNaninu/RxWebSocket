using RxWebSocket;

namespace WebSocketChat
{
    public class WebSocketWithName
    {
        public WebSocketClient WebSocketClient { get; private set; }
        public string Name { get; }

        public WebSocketWithName(WebSocketClient websocket, string name)
        {
            WebSocketClient = websocket;
            Name = name;
        }
    }
}
