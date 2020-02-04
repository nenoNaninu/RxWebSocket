using System;
using UniRx;

namespace RxWebSocket.Sample
{
    /// <summary>
    /// UIの操作
    /// 接続/切断ボタンが押されたら通知するのと、
    /// 接続できたらnameとuriのとこ使えなくする
    /// </summary>
    public class ChatPresenter : IChatPresenter, IDisposable
    {
        private readonly Subject<ConnectionRequest> _onConnectionButtonClickSubject = new Subject<ConnectionRequest>();
        private readonly Subject<Unit> _onCloseButtonClickSubject = new Subject<Unit>();
        private readonly Subject<string> _onMessageSendRequestSubject = new Subject<string>();
        private readonly IChatView _chatView;

        public IObservable<ConnectionRequest> OnConnectButtonClick => _onConnectionButtonClickSubject.AsObservable();
        public IObservable<Unit> OnCloseButtonClick => _onCloseButtonClickSubject.AsObservable();
        public IObservable<string> OnMessageSendRequest => _onMessageSendRequestSubject.AsObservable();
        public IObservable<Unit> OnDestroy => _chatView.OnDestroy;

        private string _uri;
        private string _name;

        public void OnMessageReceived(string name, string message)
        {
            _chatView.DrawNewMessage(name, message);
        }

        public void OnConnectSuccess()
        {
            _chatView.UriInputField.interactable = false;
            _chatView.NameInputField.interactable = false;
            _chatView.ChatTextInputField.interactable = true;
            _chatView.SendButton.interactable = true;
            
            _chatView.ConnectButton.gameObject.SetActive(false);
            _chatView.CloseButton.gameObject.SetActive(true);
        }

        public void OnDisconnect()
        {
            _chatView.UriInputField.interactable = true;
            _chatView.NameInputField.interactable = true;
            _chatView.ChatTextInputField.interactable = false;
            _chatView.SendButton.interactable = false;
            
            _chatView.ConnectButton.gameObject.SetActive(true);
            _chatView.CloseButton.gameObject.SetActive(false);
        }

        public ChatPresenter(IChatView chatView)
        {
            _chatView = chatView;

            _chatView.ConnectButton.onClick
                .AsObservable()
                .Subscribe(_ => _onConnectionButtonClickSubject.OnNext(new ConnectionRequest(_uri, _name)));

            _chatView.CloseButton.onClick
                .AsObservable()
                .Subscribe(e => _onCloseButtonClickSubject.OnNext(e));

            _chatView.UriInputField.OnEndEditAsObservable()
                .Subscribe(x => _uri = x);

            _chatView.NameInputField.OnEndEditAsObservable()
                .Subscribe(x => _name = x);

            _chatView.SendButton.onClick.AsObservable()
                .Subscribe(_ => _onMessageSendRequestSubject.OnNext(_chatView.ChatTextInputField.text));
            
            _chatView.UriInputField.interactable = true;
            _chatView.NameInputField.interactable = true;
            _chatView.ChatTextInputField.interactable = false;
            _chatView.SendButton.interactable = false;
            
            _chatView.ConnectButton.gameObject.SetActive(true);
            _chatView.CloseButton.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            _onConnectionButtonClickSubject?.Dispose();
            _onCloseButtonClickSubject?.Dispose();
        }
    }
}