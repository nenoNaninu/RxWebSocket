using System.Text;
using Utf8Json;
using System;

namespace WebSocketChat
{
    public class ChatHandler : WebSocketHandler
    {
        public override void OnConnected(WebSocketWithName socket)
        {
            base.OnConnected(socket);

            var bytes = JsonSerializer.Serialize(new ChatMessage(socket.Name, "now connected"));
            SendMessageToAllAsync(bytes);

            socket.WebSocketClient
                .BinaryMessageReceived
                .Subscribe(x =>
                {
                    var message = JsonSerializer.Serialize(new ChatMessage(socket.Name, Encoding.UTF8.GetString(x)));
                    Console.WriteLine($"message result {message.Length}");
                    SendMessageToAllAsync(message);
                });

            socket.WebSocketClient
                .CloseMessageReceived
                .Subscribe(_ =>
                {
                    base.OnDisconnected(socket);
                    SendMessageToAllAsync(JsonSerializer.Serialize(new ChatMessage(socket.Name, "disconnected")));
                });
        }
    }
}