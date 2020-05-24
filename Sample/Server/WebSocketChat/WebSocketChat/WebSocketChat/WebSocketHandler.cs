using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebSocketChat
{
    public abstract class WebSocketHandler
    {
        private ConcurrentDictionary<WebSocketWithName, WebSocketWithName> _webSocketCollection = new ConcurrentDictionary<WebSocketWithName, WebSocketWithName>();

        public WebSocketHandler()
        {
            Console.CancelKeyPress += (sender, args) =>
            {
                foreach (var socket in _webSocketCollection.Values)
                {
                    if (socket.WebSocketClient.IsOpen)
                    {
                        socket.WebSocketClient.CloseAsync(WebSocketCloseStatus.InternalServerError, "press ctrl+c", true).Wait();
                    }
                }
            };
        }
        public virtual void OnConnected(WebSocketWithName socket)
        {
            _webSocketCollection[socket] = socket;
            //_webSocketCollection.Add(socket);
        }

        public virtual void OnDisconnected(WebSocketWithName socket)
        {
            _webSocketCollection.TryRemove(socket, out var _);
            //_webSocketCollection.TryTake(out socket);
        }

        public void SendMessageAsync(WebSocketWithName socket, string message)
        {
            if (!socket.WebSocketClient.IsOpen)
            {
                return;
            }

            socket.WebSocketClient.Send(message);
        }

        public void SendMessageToAllAsync(string message)
        {
            foreach (var socket in _webSocketCollection.Values)
            {
                if (socket.WebSocketClient.IsOpen)
                {
                    socket.WebSocketClient.Send(message);
                }
            }
        }

        public void SendMessageAsync(WebSocketWithName socket, byte[] message)
        {
            if (!socket.WebSocketClient.IsOpen)
            {
                return;
            }

            socket.WebSocketClient.Send(message);
        }

        public void SendMessageToAllAsync(byte[] message)
        {
            foreach (var socket in _webSocketCollection.Values)
            {
                if (socket.WebSocketClient.IsOpen)
                {
                    socket.WebSocketClient.Send(message);
                }
            }
        }
    }
}