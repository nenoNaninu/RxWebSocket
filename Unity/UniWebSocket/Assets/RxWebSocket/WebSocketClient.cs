using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RxWebSocket.Exceptions;
using RxWebSocket.Threading;
using RxWebSocket.Validations;
using RxWebSocket.Logging;
using RxWebSocket.Message;
using RxWebSocket.Senders;
using RxWebSocket.Utils;

#if NETSTANDARD2_1 || NETSTANDARD2_0 || NETCOREAPP
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;
#else
using UniRx;
#endif

namespace RxWebSocket
{
    public partial class WebSocketClient : IWebSocketClient
    {
        private readonly ILogger _logger;

        private readonly MemoryPool _memoryPool;

        private readonly Func<ClientWebSocket> _clientFactory;

        private readonly AsyncLock _openLocker = new AsyncLock();
        private readonly AsyncLock _closeLocker = new AsyncLock();

        private readonly Subject<byte[]> _binaryMessageReceivedSubject = new Subject<byte[]>();
        private readonly Subject<byte[]> _textMessageReceivedSubject = new Subject<byte[]>();

        private readonly Subject<CloseMessage> _closeMessageReceivedSubject = new Subject<CloseMessage>();
        private readonly Subject<WebSocketBackgroundException> _exceptionSubject = new Subject<WebSocketBackgroundException>();
        private readonly Subject<Unit> _onDisposeSubject = new Subject<Unit>();

        private readonly CancellationTokenSource _cancellationSocketJobs = new CancellationTokenSource();

        private readonly ConcurrentQueue<WebSocketBackgroundException> _backgroundExceptionQueue = new ConcurrentQueue<WebSocketBackgroundException>();

        private readonly IWebSocketMessageSenderCore _webSocketMessageSender;

        private int _isDisposed = 0;

        private WebSocket _socket;
        private Task _listenTask;

        public Uri Url { get; }

        ///<inheritdoc/>
        public string Name { get; }

        public Encoding MessageEncoding { get; }

        public DateTime LastReceivedTime { get; private set; } = DateTime.UtcNow;

        public bool IsListening { get; private set; }

        public WebSocketClient(
            Uri url,
            WebSocketMessageSender sender = null,
            ReceiverMemoryConfig receiverConfig = null,
            ILogger logger = null,
            string name = "CLIENT",
            Encoding messageEncoding = null,
            Func<ClientWebSocket> clientFactory = null)
        {
            if (!ValidationUtils.ValidateInput(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            Url = url;
            Name = name;
            _logger = logger;
            MessageEncoding = messageEncoding ?? Encoding.UTF8;

            receiverConfig = receiverConfig ?? ReceiverMemoryConfig.Default; //cannot use =?? in unity
            _memoryPool = new MemoryPool(receiverConfig.InitialMemorySize, receiverConfig.MarginSize, logger);

            _clientFactory = clientFactory ?? DefaultClientFactory;

            _webSocketMessageSender = sender?.AsCore() ?? new SingleQueueSenderCore();
            _webSocketMessageSender.SetConfig(MessageEncoding, logger, Name);

            _webSocketMessageSender
                .ExceptionHappenedInSending
                .Subscribe(x =>
                {
                    _backgroundExceptionQueue.Enqueue(x);
                    Dispose();
                });
        }

        public WebSocketClient(
            WebSocket connectedSocket,
            WebSocketMessageSender sender = null,
            ReceiverMemoryConfig receiverConfig = null,
            ILogger logger = null,
            string name = "CLIENT",
            Encoding messageEncoding = null)
        {
            _socket = connectedSocket ?? throw new ArgumentNullException(nameof(connectedSocket));

            Url = null;
            _clientFactory = null;

            Name = name;
            _logger = logger;
            MessageEncoding = messageEncoding ?? Encoding.UTF8;

            receiverConfig = receiverConfig ?? ReceiverMemoryConfig.Default;
            _memoryPool = new MemoryPool(receiverConfig.InitialMemorySize, receiverConfig.MarginSize, logger);

            _webSocketMessageSender = sender?.AsCore() ?? new SingleQueueSenderCore();
            _webSocketMessageSender.SetConfig(MessageEncoding, logger, Name);

            _webSocketMessageSender
                .ExceptionHappenedInSending
                .Subscribe(x =>
                {
                    _backgroundExceptionQueue.Enqueue(x);
                    Dispose();
                });
        }

        public WebSocket NativeSocket => _socket;

        public bool IsOpen => _socket != null && _socket.State == WebSocketState.Open;
        public bool IsClosed => _socket != null && _socket.State == WebSocketState.Closed;

        public WebSocketState WebSocketState => _socket?.State ?? WebSocketState.None;

        public bool IsDisposed => 0 < _isDisposed;

        public IObservable<byte[]> BinaryMessageReceived => _binaryMessageReceivedSubject.AsObservable();

        public IObservable<byte[]> RawTextMessageReceived => _textMessageReceivedSubject.AsObservable();

        public IObservable<string> TextMessageReceived => _textMessageReceivedSubject.Select(MessageEncoding.GetString);

        public IObservable<Unit> OnDispose => _onDisposeSubject.AsObservable();

        ///<inheritdoc/>
        public IObservable<CloseMessage> CloseMessageReceived => _closeMessageReceivedSubject.AsObservable();

        public IObservable<WebSocketBackgroundException> ExceptionHappenedInBackground => _exceptionSubject.AsObservable();


        ///<inheritdoc/>
        public async Task ConnectAsync()
        {
            using (await _openLocker.LockAsync().ConfigureAwait(false))
            {
                if (IsListening)
                {
                    _logger?.Warn(FormatLogMessage("WebSocketClient is already open and receiving"));
                    return;
                }

                if (IsDisposed)
                {
                    _logger?.Error(FormatLogMessage("WebSocketClient is already disposed."));
                    throw new ObjectDisposedException("WebSocketClient is already disposed.");
                }

                var connectionTask = ConnectAsyncCore();

                _webSocketMessageSender.StartSendingMessageFromQueue();

                await connectionTask.ConfigureAwait(false);
            }
        }

        private async Task ConnectAsyncCore()
        {
            try
            {
                if (_socket == null)
                {
                    _logger?.Log(FormatLogMessage("Connecting..."));
                    var client = _clientFactory();
                    await client.ConnectAsync(Url, _cancellationSocketJobs.Token).ConfigureAwait(false);
                    _socket = client;
                }

                _webSocketMessageSender.SetSocket(_socket);

                _logger?.Log(FormatLogMessage("Start Listening..."));

                _listenTask = Task.Run(Listen);
                IsListening = true;
                LastReceivedTime = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                _logger?.Error(e, FormatLogMessage($"Exception while connecting. detail: {e.Message}"));
                throw;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref _isDisposed) != 1)
            {
                return;
            }

            try
            {
                using (_onDisposeSubject)
                using (_exceptionSubject)
                using (_closeMessageReceivedSubject)
                using (_textMessageReceivedSubject)
                using (_binaryMessageReceivedSubject)
                using (_cancellationSocketJobs)
                using (_socket)
                {
                    _logger?.Log(FormatLogMessage("Disposing..."));

                    _webSocketMessageSender.Dispose();

                    if (!IsClosed)
                    {
                        try
                        {
                            _socket?.Abort();
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    try
                    {
                        foreach (var item in _backgroundExceptionQueue)
                        {
                            _exceptionSubject.OnNext(item);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger?.Error(e, FormatLogMessage($"An exception occurred in ExceptionHappenedInBackground.OnNext() : {e.Message}"));
                    }

                    _cancellationSocketJobs.CancelWithoutException();

                    try
                    {
                        _onDisposeSubject.OnNext(Unit.Default);
                    }
                    catch (Exception e)
                    {
                        _logger?.Error(e, FormatLogMessage($"An exception occurred in OnDispose.OnNext() : {e.Message}"));
                    }

                    try
                    {
                        _exceptionSubject.OnCompleted();
                        _binaryMessageReceivedSubject.OnCompleted();
                        _textMessageReceivedSubject.OnCompleted();
                        _closeMessageReceivedSubject.OnCompleted();
                        _onDisposeSubject.OnCompleted();
                    }
                    catch (Exception e)
                    {
                        _logger?.Error(e, FormatLogMessage($"An exception occurred in OnCompleted() : {e.Message}"));
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, FormatLogMessage($"Failed to dispose client, error : {e.Message}"));
            }
        }

        ///<inheritdoc/>
        public Task CloseAsync(WebSocketCloseStatus status, string statusDescription)
        {
            return CloseAsync(status, statusDescription, true);
        }

        ///<inheritdoc/>
        public async Task CloseAsync(WebSocketCloseStatus status, string statusDescription, bool dispose)
        {
            // prevent sending multiple disconnect requests.
            using (await _closeLocker.LockAsync().ConfigureAwait(false))
            {
                if (IsClosed)
                {
                    return;
                }

                if (_socket == null ||
                    _socket.State == WebSocketState.Aborted ||
                    _socket.State == WebSocketState.None)
                {
                    _logger?.Warn(FormatLogMessage($"Called CloseAsync, but websocket state is {(_socket == null ? "null" : _socket.State.ToStringFast())}. It is not correct."));
                    return;
                }

                try
                {
                    await _webSocketMessageSender.StopAsync().ConfigureAwait(false);

                    // await until the connection closed.
                    await _socket.CloseAsync(status, statusDescription, _cancellationSocketJobs.Token)
                        .ConfigureAwait(false);
                    IsListening = false;

                    _cancellationSocketJobs.CancelWithoutException();
                }
                catch (Exception e)
                {
                    _logger?.Error(e, FormatLogMessage($"Error while closing client, message : '{e.Message}'"));
                    throw;
                }
                finally
                {
                    if (dispose)
                    {
                        Dispose();
                    }
                }
            }
        }

        private async ValueTask<bool> PublishMessage(WebSocketReceiveResult result)
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var receivedText = _memoryPool.ToArray();
                _logger?.Log(FormatLogMessage($"Received: Type Text, binary length: {receivedText.Length}"));
                _textMessageReceivedSubject.OnNext(receivedText);
            }
            else if (result.MessageType == WebSocketMessageType.Binary)
            {
                var receivedData = _memoryPool.ToArray();
                _logger?.Log(FormatLogMessage($"Received: Type Binary, binary length: {receivedData.Length}"));
                _binaryMessageReceivedSubject.OnNext(receivedData);
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                //close handshake
                _logger?.Log(FormatLogMessage($"Received: Close Message, Status: {result.CloseStatus.ToStringFast()}, Description: {result.CloseStatusDescription}"));
                _closeMessageReceivedSubject.OnNext(new CloseMessage(result.CloseStatus, result.CloseStatusDescription));
                try
                {
                    await CloseAsync(WebSocketCloseStatus.NormalClosure,
                        $"Response to the close message. Received close status: {result.CloseStatus.ToStringFast()}",
                        true).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger?.Error(e,
                        FormatLogMessage(
                            $"Close message was received, so trying to close socket, but exception occurred. error: '{e.Message}'"));

                    _backgroundExceptionQueue.Enqueue(new WebSocketBackgroundException(e, ExceptionType.CloseMessageReceive));
                    Dispose();
                }

                return true;
            }

            return false;
        }

        private async Task Listen()
        {
            try
            {
                do
                {
                    _memoryPool.Offset = 0;
                    var memorySegment = _memoryPool.SliceFromOffset();

                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _socket.ReceiveAsync(memorySegment, _cancellationSocketJobs.Token).ConfigureAwait(false);

                        if (result.MessageType != WebSocketMessageType.Close)
                        {
                            _memoryPool.Offset += result.Count;
                            memorySegment = _memoryPool.SliceFromOffset();
                        }
                    }
                    while (!result.EndOfMessage);

                    LastReceivedTime = DateTime.UtcNow;

                    if (await PublishMessage(result))
                    {
                        return;
                    }
                }
                while (_socket.State == WebSocketState.Open && !_cancellationSocketJobs.IsCancellationRequested);
            }
            catch (TaskCanceledException)
            {
                // task was canceled, ignore
            }
            catch (OperationCanceledException)
            {
                // operation was canceled, ignore
            }
            catch (ObjectDisposedException)
            {
                // client was disposed, ignore
            }
            catch (Exception e)
            {
                _logger?.Error(e, FormatLogMessage($"Error while listening to websocket stream, error: '{e.Message}'"));
                _backgroundExceptionQueue.Enqueue(new WebSocketBackgroundException(e, ExceptionType.Listen));
                Dispose();
            }
        }

        public Task WaitUntilCloseAsync()
        {
            if (_listenTask != null)
            {
                return _listenTask;
            }

            throw new RxWebSocket.Exceptions.WebSocketException("Call ConnectAsync() before calling this function.");
        }

        private string FormatLogMessage(string msg)
        {
            return $"[WEBSOCKET {Name}] {msg}";
        }

        private static readonly Func<ClientWebSocket> DefaultClientFactory = () => new ClientWebSocket
        {
            Options = { KeepAliveInterval = TimeSpan.FromSeconds(5) }
        };
    }
}