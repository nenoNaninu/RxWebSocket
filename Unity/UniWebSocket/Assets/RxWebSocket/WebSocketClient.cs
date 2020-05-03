using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RxWebSocket.Threading;
using RxWebSocket.Validations;
using RxWebSocket.Logging;

#if NETSTANDARD2_1 || NETSTANDARD2_0
using System.Reactive.Subjects;
using System.Reactive.Linq;
#else
using UniRx;
#endif

namespace RxWebSocket
{
    public partial class WebSocketClient : IWebSocketClient
    {
        #region Member variable with state.
        private readonly ILogger _logger;

        private readonly MemoryPool _memoryPool;

        private readonly Func<ClientWebSocket> _clientFactory;

        private readonly AsyncLock _openLocker = new AsyncLock();
        private readonly AsyncLock _closeLocker = new AsyncLock();

        private readonly Subject<byte[]> _binaryMessageReceivedSubject = new Subject<byte[]>();
        private readonly Subject<byte[]> _textMessageReceivedSubject = new Subject<byte[]>();

        private readonly Subject<CloseMessage> _closeMessageReceivedSubject = new Subject<CloseMessage>();
        private readonly Subject<WebSocketExceptionDetail> _exceptionSubject = new Subject<WebSocketExceptionDetail>();

        private readonly CancellationTokenSource _cancellationCurrentJobs = new CancellationTokenSource();
        private readonly CancellationTokenSource _cancellationAllJobs = new CancellationTokenSource();

        private readonly IWebSocketMessageSenderCore _webSocketMessageSender;

        private WebSocket _socket;
        private bool _isAlreadyReceiving;

        public Uri Url { get; }

        /// <summary>
        /// For logging purpose.
        /// </summary>
        public string Name { get; }

        public bool IsDisposed { get; private set; }

        public Encoding MessageEncoding { get; }

        public DateTime LastReceivedTime { get; private set; } = DateTime.UtcNow;

        public Task WaitUntilClose { get; private set; }
        #endregion

        public WebSocketClient(
            Uri url,
            WebSocketMessageSender messageSender = null,
            ReceivingMemoryConfig receivingMemoryConfig = null,
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
            MessageEncoding = messageEncoding ?? Encoding.UTF8;
            _logger = logger;

            receivingMemoryConfig = receivingMemoryConfig ?? ReceivingMemoryConfig.Default; //cannot use =?? in unity
            _memoryPool = new MemoryPool(receivingMemoryConfig.InitialMemorySize, receivingMemoryConfig.MarginSize, logger);

            _clientFactory = clientFactory ?? MakeDefaultClientFactory();

            _webSocketMessageSender = messageSender?.AsCore() ?? new SingleQueueSenderCore();
            _webSocketMessageSender.SetInternal(_cancellationCurrentJobs.Token, _cancellationAllJobs.Token, logger, Name);

            _webSocketMessageSender
                .ExceptionHappenedInSending
                .Subscribe(_exceptionSubject.OnNext);
        }

        public WebSocketClient(
            WebSocket connectedSocket,
            WebSocketMessageSender messageSender = null,
            ReceivingMemoryConfig receivingMemoryConfig = null,
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

            receivingMemoryConfig = receivingMemoryConfig ?? ReceivingMemoryConfig.Default;
            _memoryPool = new MemoryPool(receivingMemoryConfig.InitialMemorySize, receivingMemoryConfig.MarginSize, logger);

            _webSocketMessageSender = messageSender?.AsCore() ?? new SingleQueueSenderCore();
            _webSocketMessageSender.SetInternal(_cancellationCurrentJobs.Token, _cancellationAllJobs.Token, logger, Name);

            _webSocketMessageSender
                .ExceptionHappenedInSending
                .Subscribe(_exceptionSubject.OnNext);
        }

        public WebSocket NativeSocket => _socket;

        public bool IsOpen => _socket != null && _socket.State == WebSocketState.Open;
        public bool IsClosed => _socket != null && _socket.State == WebSocketState.Closed;

        public WebSocketState WebSocketState => _socket?.State ?? WebSocketState.None;

        public IObservable<byte[]> BinaryMessageReceived => _binaryMessageReceivedSubject.AsObservable();

        public IObservable<byte[]> RawTextMessageReceived => _textMessageReceivedSubject.AsObservable();

        public IObservable<string> TextMessageReceived => RawTextMessageReceived.Select(MessageEncoding.GetString);

        /// Invoke when a close message is received,
        /// before disconnecting the connection in normal system.
        public IObservable<CloseMessage> CloseMessageReceived => _closeMessageReceivedSubject.AsObservable();

        public IObservable<WebSocketExceptionDetail> ExceptionHappened => _exceptionSubject.AsObservable();

        /// <summary>
        /// Start connect and listening to the websocket stream on the background thread
        /// </summary>
        public async Task ConnectAndStartListening()
        {
            using (await _openLocker.LockAsync().ConfigureAwait(false))
            {
                if (_isAlreadyReceiving)
                {
                    _logger?.Warn(FormatLogMessage("WebSocketClient is already open and receiving"));
                    return;
                }

                if (IsDisposed)
                {
                    _logger?.Error(FormatLogMessage("WebSocketClient is already disposed."));
                    throw new ObjectDisposedException("WebSocketClient is already disposed.");
                }

                var connectionTask = ConnectAndStartListeningInternal(Url, _cancellationCurrentJobs.Token);

                StartBackgroundThreadForSendingMessage();

                await connectionTask.ConfigureAwait(false);
            }
        }

        private async Task ConnectAndStartListeningInternal(Uri uri, CancellationToken token)
        {
            try
            {
                if (_socket == null)
                {
                    _logger?.Log(FormatLogMessage("Connecting..."));
                    var client = _clientFactory();
                    await client.ConnectAsync(uri, token).ConfigureAwait(false);
                    _socket = client;
                }

                _webSocketMessageSender.SetSocket(_socket);

                _logger?.Log(FormatLogMessage("Start Listening..."));

                WaitUntilClose = Listen(_socket, token);
                _isAlreadyReceiving = true;
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
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            _logger?.Log(FormatLogMessage("Disposing..."));

            try
            {
                _cancellationAllJobs.Cancel();
                _cancellationCurrentJobs.Cancel();

                if (!IsClosed)
                {
                    _socket?.Abort();
                }

                _socket?.Dispose();

                _cancellationAllJobs.Dispose();
                _cancellationCurrentJobs.Dispose();

                _webSocketMessageSender.Dispose();

                _binaryMessageReceivedSubject.Dispose();
                _textMessageReceivedSubject.Dispose();
                _closeMessageReceivedSubject.Dispose();
                _exceptionSubject.Dispose();
            }
            catch (Exception e)
            {
                _logger?.Error(e, FormatLogMessage($"Failed to dispose client, error: {e.Message}"));
                throw;
            }
        }

        /// <summary>
        /// close websocket connection.
        /// </summary>
        public Task CloseAsync(WebSocketCloseStatus status, string statusDescription)
        {
            return CloseAsync(status, statusDescription, true);
        }

        /// <summary>
        /// close websocket connection.
        /// </summary>
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
                    _logger?.Warn(FormatLogMessage($"Called CloseAsync, but websocket state is {(_socket == null ? "null" : _socket.State.ToString())}. It is not correct."));
                    return;
                }

                try
                {
                    // await until the connection closed.
                    await _socket.CloseAsync(status, statusDescription, _cancellationCurrentJobs.Token).ConfigureAwait(false);

                    if (dispose)
                    {
                        this.Dispose();
                    }
                }
                catch (Exception e)
                {
                    _logger?.Error(e, FormatLogMessage($"Error while closing client, message: '{e.Message}'"));
                    throw;
                }
            }
        }

        private static Func<ClientWebSocket> MakeDefaultClientFactory()
        {
            return () => new ClientWebSocket
            {
                Options = { KeepAliveInterval = TimeSpan.FromSeconds(5) }
            };
        }

        private async Task Listen(WebSocket client, CancellationToken token)
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
                        result = await client.ReceiveAsync(memorySegment, token).ConfigureAwait(false);

                        if (result.MessageType != WebSocketMessageType.Close)
                        {
                            _memoryPool.Offset += result.Count;
                            memorySegment = _memoryPool.SliceFromOffset();
                        }
                    }
                    while (!result.EndOfMessage);

                    LastReceivedTime = DateTime.UtcNow;

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var receivedText = _memoryPool.ToArray();
                        _logger?.Log(FormatLogMessage($"Received: Type Text: {receivedText}"));
                        _textMessageReceivedSubject.OnNext(receivedText);
                    }
                    else if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        var dstArray = _memoryPool.ToArray();
                        _logger?.Log(FormatLogMessage($"Received: Type Binary, length: {dstArray?.Length}"));
                        _binaryMessageReceivedSubject.OnNext(dstArray);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        //close handshake
                        if (result.CloseStatus != null)
                        {
                            _logger?.Log(FormatLogMessage($"Received: Close Message, Status: {result.CloseStatus.Value}, Description: {result.CloseStatusDescription}"));
                            _closeMessageReceivedSubject.OnNext(new CloseMessage(result.CloseStatus.Value, result.CloseStatusDescription));
                            try
                            {
                                await CloseAsync(WebSocketCloseStatus.NormalClosure, $"Response to the close message. Received close status: {result.CloseStatus.Value}", true).ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                _logger?.Error(e, FormatLogMessage($"Close message was received, so trying to close socket, but exception occurred. error: '{e.Message}'"));
                                if (!IsDisposed)
                                {
                                    _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.CloseMessageReceive));
                                }
                            }
                        }
                        return;
                    }
                }
                while (client.State == WebSocketState.Open && !token.IsCancellationRequested);
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
                if (!IsDisposed)
                {
                    _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.Listen));
                }
            }
        }

        private string FormatLogMessage(string msg)
        {
            return $"[WEBSOCKET {Name}] {msg}";
        }
    }
}