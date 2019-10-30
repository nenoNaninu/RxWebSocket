using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace WebSocketChat
{
    public class ChatMessageHandler : WebSocketHandler
    {
        public ChatMessageHandler(WebSocketObjectHolder webSocketObjectHolder) : base(webSocketObjectHolder)
        {
        }

        public override async Task OnConnected(WebSocketWithName socket)
        {
            await base.OnConnected(socket);

            var socketid = WebSocketObjectHolder.GetId(socket);
            var bytes = JsonSerializer.Serialize(new ChatMessage(socket.Name, "now connected"));
            await SendMessageToAllAsync(bytes);
        }

        public override async Task ReceiveAsync(WebSocketWithName socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var message = JsonSerializer.Serialize(new ChatMessage(socket.Name, Encoding.UTF8.GetString(buffer)));

            await SendMessageToAllAsync(message);
        }
        
        public override async Task OnDisconnected(WebSocketWithName socket)
        {
            var socketId = WebSocketObjectHolder.GetId(socket);

            await base.OnDisconnected(socket);
            await SendMessageToAllAsync(JsonSerializer.Serialize(new ChatMessage(socket.Name, "disconnected")));
        }
    }
}
