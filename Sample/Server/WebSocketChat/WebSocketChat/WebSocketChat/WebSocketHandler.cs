using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebSocketChat
{
    public abstract class WebSocketHandler
    {
        private BlockingCollection<WebSocketWithName> _webSocketCollection = new BlockingCollection<WebSocketWithName>();

        public WebSocketHandler()
        {
            Console.CancelKeyPress += (sender, args) =>
            {
                foreach (var socket in _webSocketCollection)
                {
                    socket.WebSocketClient.CloseAsync(WebSocketCloseStatus.InternalServerError, "press ctrl+c", true).Wait();
                }
            };
        }
        public virtual void OnConnected(WebSocketWithName socket)
        {
            _webSocketCollection.Add(socket);
        }

        public virtual void OnDisconnected(WebSocketWithName socket)
        {
            _webSocketCollection.TryTake(out socket);
        }

        public void SendMessageAsync(WebSocketWithName socket, string message)
        {
            if (!socket.WebSocketClient.IsConnected)
            {
                return;
            }

            socket.WebSocketClient.Send(message);
        }

        public void SendMessageToAllAsync(string message)
        {
            foreach (var socket in _webSocketCollection)
            {
                if (socket.WebSocketClient.IsConnected)
                {
                    socket.WebSocketClient.Send(message);
                }
            }
        }

        public void SendMessageAsync(WebSocketWithName socket, byte[] message)
        {
            if (!socket.WebSocketClient.IsConnected)
            {
                return;
            }

            socket.WebSocketClient.Send(message);
        }

        public void SendMessageToAllAsync(byte[] message)
        {
            foreach (var socket in _webSocketCollection)
            {
                if (socket.WebSocketClient.IsConnected)
                {
                    socket.WebSocketClient.Send(message);
                }
            }
        }
    }
}