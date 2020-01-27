using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using UniWebSocket;

namespace UniWebSocket.Sample
{
    public interface IChatClient : IDisposable
    {
        Task Connect(string name, string uri);
        Task Close();
        Task Send(string message);
        IObservable<ChatMessage> OnReceived { get; }
        IObservable<WebSocketCloseStatus> OnError { get; }
    }
}