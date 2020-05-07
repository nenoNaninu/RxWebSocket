using System;
using System.Threading.Tasks;
using RxWebSocket.Message;

namespace RxWebSocket.Sample
{
    public interface IChatClient : IDisposable
    {
        Task Connect(string name, string uri);
        Task Close();
        Task Send(string message);
        IObservable<ChatMessage> OnReceived { get; }
        IObservable<CloseMessage> OnError { get; }
    }
}