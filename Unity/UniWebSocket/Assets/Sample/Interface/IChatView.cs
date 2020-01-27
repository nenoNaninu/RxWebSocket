using System;
using UniRx;
using UnityEngine.UI;

namespace UniWebSocket.Sample
{
    public interface IChatView
    {
        InputField UriInputField { get; }
        InputField NameInputField { get; }
        InputField ChatTextInputField { get; }

        Button ConnectButton { get; }
        Button CloseButton { get; }
        Button SendButton { get; }

        void DrawNewMessage(string name, string message);
        
        IObservable<Unit> OnDestroy { get; }
    }
}