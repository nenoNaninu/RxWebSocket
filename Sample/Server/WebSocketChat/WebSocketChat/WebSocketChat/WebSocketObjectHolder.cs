using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketChat
{
    public class WebSocketObjectHolder
    {
        private ConcurrentDictionary<string, WebSocketWithName> _sockets = new ConcurrentDictionary<string, WebSocketWithName>();

        public WebSocketWithName GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public ConcurrentDictionary<string, WebSocketWithName> GetAll()
        {
            return _sockets;
        }

        public string GetId(WebSocketWithName socket)
        {
            return _sockets.FirstOrDefault(p => p.Value == socket).Key;
        }
        public void AddSocket(WebSocketWithName socket)
        {
            _sockets.TryAdd(CreateConnectionId(), socket);
        }

        public async Task RemoveSocket(string id)
        {
            WebSocketWithName socket;
            _sockets.TryRemove(id, out socket);

            await socket.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the WebSocketManager", CancellationToken.None);
        }

        private string CreateConnectionId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}