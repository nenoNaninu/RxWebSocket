using UniRx;

namespace RxWebSocket.Sample
{
    public class ChatUseCase
    {
        private readonly IChatPresenter _presenter;
        private readonly IChatClient _chatClient;

        public ChatUseCase(IChatPresenter presenter, IChatClient chatClient)
        {
            _presenter = presenter;
            _chatClient = chatClient;
            

            _presenter.OnConnectButtonClick
                .Where(x => !string.IsNullOrEmpty(x.Name) && !string.IsNullOrEmpty(x.Uri))
                .Subscribe(async x =>
                {
                    await chatClient.Connect(x.Name, x.Uri);
                    _presenter.OnConnectSuccess();
                });

            _presenter.OnCloseButtonClick
                .Subscribe(async _ =>
                {
                    await _chatClient.Close();
                    presenter.OnDisconnect();
                });

            _presenter.OnMessageSendRequest
                .Subscribe(x => chatClient.Send(x));

            _chatClient.OnReceived
                .ObserveOnMainThread()
                .Subscribe(x => presenter.OnMessageReceived(x.Name, x.Message));

            _presenter.OnDestroy
                .Subscribe( _ =>
                {
                    _chatClient.Dispose();
                });
        }
    }
}