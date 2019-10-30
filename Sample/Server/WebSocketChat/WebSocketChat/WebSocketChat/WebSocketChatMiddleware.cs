using System;
using System.Buffers;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebSocketChat
{
    public class WebSocketChatMiddleware
    {
        private readonly RequestDelegate _next;
        private WebSocketHandler _webSocketHandler { get; set; }

        public WebSocketChatMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest) return;

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            
            var buffer = ArrayPool<byte>.Shared.Rent(1024 * 8);
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var name = Encoding.UTF8.GetString(buffer, 0, result.Count);
            ArrayPool<byte>.Shared.Return(buffer,true);
            var socketWithName = new WebSocketWithName(socket, name);

            await _webSocketHandler.OnConnected(socketWithName);

            await Receive(socketWithName, async (result, buffer) =>
            {
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    await _webSocketHandler.ReceiveAsync(socketWithName, result, buffer);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocketHandler.OnDisconnected(socketWithName);
                }
            });
        }

        public async Task Receive(WebSocketWithName socketWithName, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new ArraySegment<byte>(new byte[1024 * 8]);

            while (socketWithName.Socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                var ms = new MemoryStream();
                do
                {
                    result = await socketWithName.Socket.ReceiveAsync(buffer, CancellationToken.None);
                    if (buffer.Array != null)
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                handleMessage(result, ms.ToArray());
            }
        }
    }
}