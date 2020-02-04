using System;
using UniRx;

namespace RxWebSocket.Sample
{
    public interface IChatPresenter
    {
        IObservable<ConnectionRequest> OnConnectButtonClick { get; }
        IObservable<Unit> OnCloseButtonClick { get; }
        
        IObservable<string> OnMessageSendRequest { get; }
        
        IObservable<Unit> OnDestroy { get; }

        void OnMessageReceived(string name, string message);
        void OnConnectSuccess();

        void OnDisconnect();
    }
}