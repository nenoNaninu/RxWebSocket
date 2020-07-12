using System;
using System.Buffers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RxWebSocket;
using RxWebSocket.Extensions;

namespace WebSocketChat
{
    public class WebSocketChatMiddleware
    {
        private readonly RequestDelegate _next;
        private WebSocketHandler _webSocketHandler { get; set; }

        private ILogger<WebSocketChatMiddleware> _logger;

        public WebSocketChatMiddleware(RequestDelegate next, WebSocketHandler webSocketHandler, ILogger<WebSocketChatMiddleware> logger)
        {
            _next = next;
            _webSocketHandler = webSocketHandler;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest) return;

            var socket = await context.WebSockets.AcceptWebSocketAsync();

            var buffer = ArrayPool<byte>.Shared.Rent(1024 * 8);
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var name = Encoding.UTF8.GetString(buffer, 0, result.Count);
            ArrayPool<byte>.Shared.Return(buffer, true);

            using var rxSocket = new WebSocketClient(socket, logger:_logger.AsWebSocketLogger(), name: name +"_server");

            await rxSocket.ConnectAsync();

            var socketWithName = new WebSocketWithName(rxSocket, name);

            _webSocketHandler.OnConnected(socketWithName);

            //If you do not wait here, the connection will be disconnected.
            await rxSocket.WaitUntilCloseAsync();
        }
    }
}
