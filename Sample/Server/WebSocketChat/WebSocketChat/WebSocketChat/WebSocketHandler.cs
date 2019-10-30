using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketChat
{
    public abstract class WebSocketHandler
    {
        protected WebSocketObjectHolder WebSocketObjectHolder { get; set; }

        public WebSocketHandler(WebSocketObjectHolder webSocketObjectHolder)
        {
            WebSocketObjectHolder = webSocketObjectHolder;
        }

        public virtual Task OnConnected(WebSocketWithName socket)
        {
            WebSocketObjectHolder.AddSocket(socket);
            return Task.CompletedTask;
        }

        public virtual async Task OnDisconnected(WebSocketWithName socket)
        {
            await WebSocketObjectHolder.RemoveSocket(WebSocketObjectHolder.GetId(socket));
        }

        public async Task SendMessageAsync(WebSocketWithName socket, string message)
        {
            if(socket.Socket.State != WebSocketState.Open) return;

            var buffer = Encoding.UTF8.GetBytes(message);

            await socket.Socket.SendAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }

        public async Task SendMessageAsync(string socketId, string message)
        {
           await SendMessageAsync(WebSocketObjectHolder.GetSocketById(socketId), message);
        }

        public async Task SendMessageToAllAsync(string message)
        {
            foreach (var pair in WebSocketObjectHolder.GetAll())
            {
                if (pair.Value.Socket.State == WebSocketState.Open)
                {
                    await SendMessageAsync(pair.Value, message);
                }
            }
        }
        
        public async Task SendMessageAsync(WebSocketWithName socket, byte[] message)
        {
            if(socket.Socket.State != WebSocketState.Open) return;

            await socket.Socket.SendAsync(new ArraySegment<byte>(message, 0, message.Length), WebSocketMessageType.Binary, true,
                CancellationToken.None);
        }

        public async Task SendMessageAsync(string socketId, byte[] message)
        {
            await SendMessageAsync(WebSocketObjectHolder.GetSocketById(socketId), message);
        }

        public async Task SendMessageToAllAsync(byte[] message)
        {
            foreach (var pair in WebSocketObjectHolder.GetAll())
            {
                if (pair.Value.Socket.State == WebSocketState.Open)
                {
                    await SendMessageAsync(pair.Value, message);
                }
            }
        }

        public abstract Task ReceiveAsync(WebSocketWithName socket, WebSocketReceiveResult result, byte[] buffer);
    }
}
