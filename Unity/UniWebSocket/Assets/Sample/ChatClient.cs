using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using RxWebSocket;
using RxWebSocket.Logging;
using UniRx;
using Utf8Json;

namespace RxWebSocket.Sample
{
    public class ChatClient : IChatClient
    {
        private IWebSocketClient _webSocketClient;
        private readonly ILogger _logger;
        private readonly Subject<ChatMessage> _receivedSubject = new Subject<ChatMessage>();
        private readonly Subject<CloseMessage> _closeSubject = new Subject<CloseMessage>();
        public IObservable<ChatMessage> OnReceived => _receivedSubject.AsObservable();
        public IObservable<CloseMessage> OnError => _closeSubject.AsObservable();

        public ChatClient(ILogger logger = null)
        {
            _logger = logger;
        }

        public async Task Connect(string name, string uri)
        {
            var channel = Channel.CreateBounded<SentMessage>(new BoundedChannelOptions(5) { SingleReader = true, SingleWriter = false });
            _webSocketClient = new WebSocketClient(new Uri(uri), logger: _logger, messageSender: new SingleQueueSender(channel), name: name);

            _webSocketClient.BinaryMessageReceived
                .Select(bin => JsonSerializer.Deserialize<ChatMessage>(bin))
                .Subscribe(x => _receivedSubject.OnNext(x));

            _webSocketClient.CloseMessageReceived
                .Do(x => _logger?.Log($"CloseMessageReceived.Do()...{x}"))
                .Subscribe(x => _closeSubject.OnNext(x));

            _webSocketClient.ExceptionHappened
                .Subscribe(x =>
                {
                    _logger?.Log("exception stream...");
                    _logger?.Log(x.ErrorType.ToString());
                    _logger?.Log(x.Exception.ToString());
                });

            await _webSocketClient.ConnectAndStartListening();
            _webSocketClient.Send(Encoding.UTF8.GetBytes(name));
        }

        public async Task Close()
        {
            if (_webSocketClient != null)
            {
                _logger?.Log("ChatClient will be closed!!");
                var closeTask = _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal");
                _logger?.Log($"ChatClient: {_webSocketClient.WebSocketState.ToString()}");
                await closeTask;
            }
        }

        public Task Send(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            _webSocketClient.Send(bytes);
            _logger.Log($"bytes array length: {bytes.Length}");
            return Task.CompletedTask;
        }

        public async void Dispose()
        {
            await Close();
            _receivedSubject?.Dispose();
            _closeSubject?.Dispose();
        }
    }
}