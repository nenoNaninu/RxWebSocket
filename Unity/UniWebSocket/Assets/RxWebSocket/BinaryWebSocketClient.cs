using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using RxWebSocket.Exceptions;
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
    public partial class BinaryWebSocketClient : IWebSocketClient
    {
        #region Member variable with state.
        private readonly ILogger _logger;

        private readonly MemoryPool _memoryPool;

        private readonly Func<Uri, CancellationToken, Task<WebSocket>> _connectionFactory;

        private readonly AsyncLock _openLocker = new AsyncLock();
        private readonly AsyncLock _sendLocker = new AsyncLock();
        private readonly AsyncLock _closeLocker = new AsyncLock();

        private readonly Subject<byte[]> _binaryMessageReceivedSubject = new Subject<byte[]>();
        private readonly Subject<byte[]> _textMessageReceivedSubject = new Subject<byte[]>();

        private readonly Subject<CloseMessage> _closeMessageReceivedSubject = new Subject<CloseMessage>();
        private readonly Subject<WebSocketExceptionDetail> _exceptionSubject = new Subject<WebSocketExceptionDetail>();

        private readonly Channel<ArraySegment<byte>> _sentMessageQueue;
        private readonly ChannelReader<ArraySegment<byte>> _sentMessageQueueReader;
        private readonly ChannelWriter<ArraySegment<byte>> _sentMessageQueueWriter;

        private readonly CancellationTokenSource _cancellationCurrentJobs = new CancellationTokenSource();
        private readonly CancellationTokenSource _cancellationAllJobs = new CancellationTokenSource();

        private WebSocket _socket;

        public Uri Url { get; }

        /// <summary>
        /// For logging purpose.
        /// </summary>
        public string Name { get; set; } = "CLIENT";

        public bool IsDisposed { get; private set; }

        public Encoding MessageEncoding { get; set; } = Encoding.UTF8;

        public DateTime LastReceivedTime { get; private set; } = DateTime.UtcNow;

        public Task WaitUntilClose { get; private set; }
        #endregion

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        /// <param name="sendMessageQueue"></param>
        public BinaryWebSocketClient(Uri url, Channel<ArraySegment<byte>> sendMessageQueue = null, Func<ClientWebSocket> clientFactory = null)
            : this(url, ReceivingMemoryConfig.Default, sendMessageQueue, null, MakeConnectionFactory(clientFactory))
        {
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="logger"></param>
        /// <param name="sendMessageQueue"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public BinaryWebSocketClient(Uri url, ILogger logger, Channel<ArraySegment<byte>> sendMessageQueue = null, Func<ClientWebSocket> clientFactory = null)
            : this(url, ReceivingMemoryConfig.Default, sendMessageQueue, logger, MakeConnectionFactory(clientFactory))
        {
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="receivingMemoryConfig"></param>
        /// <param name="logger"></param>
        /// <param name="sendMessageQueue"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public BinaryWebSocketClient(Uri url, ReceivingMemoryConfig receivingMemoryConfig, ILogger logger = null, Channel<ArraySegment<byte>> sendMessageQueue = null, Func<ClientWebSocket> clientFactory = null)
            : this(url, receivingMemoryConfig, sendMessageQueue, logger, MakeConnectionFactory(clientFactory))
        {
        }

        public BinaryWebSocketClient(
            Uri url,
            ReceivingMemoryConfig receivingMemoryConfig,
            Channel<ArraySegment<byte>> sendMessageQueue,
            ILogger logger,
            Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
        {
            if (!ValidationUtils.ValidateInput(url))
            {
                throw new WebSocketBadInputException($"url is null. Please correct it.");
            }

            Url = url;

            _logger = logger;
            _memoryPool = new MemoryPool(receivingMemoryConfig.InitialMemorySize, receivingMemoryConfig.MarginSize, logger);

            _connectionFactory = connectionFactory ?? (async (uri, token) =>
            {
                var client = new ClientWebSocket
                {
                    Options = { KeepAliveInterval = new TimeSpan(0, 0, 0, 10) }
                };
                await client.ConnectAsync(uri, token).ConfigureAwait(false);
                return client;
            });

            _sentMessageQueue = sendMessageQueue ?? Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            _sentMessageQueueReader = _sentMessageQueue.Reader;
            _sentMessageQueueWriter = _sentMessageQueue.Writer;
        }

        /// <summary>
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="connectedSocket">Already connected socket.</param>
        /// <param name="sendMessageQueue"></param>
        public BinaryWebSocketClient(WebSocket connectedSocket, ILogger logger = null, Channel<ArraySegment<byte>> sendMessageQueue = null)
        {
            Url = null;

            _logger = logger;
            _connectionFactory = (uri, token) => Task.FromResult(connectedSocket);

            _memoryPool = new MemoryPool(ReceivingMemoryConfig.Default.InitialMemorySize, ReceivingMemoryConfig.Default.MarginSize, logger);

            _sentMessageQueue = sendMessageQueue ?? Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            _sentMessageQueueReader = _sentMessageQueue.Reader;
            _sentMessageQueueWriter = _sentMessageQueue.Writer;
        }

        /// <summary>
        /// </summary>
        /// <param name="receivingMemoryConfig"></param>
        /// <param name="logger"></param>
        /// <param name="connectedSocket">Already connected socket.</param>
        /// <param name="sendMessageQueue"></param>
        public BinaryWebSocketClient(WebSocket connectedSocket, ReceivingMemoryConfig receivingMemoryConfig, ILogger logger = null, Channel<ArraySegment<byte>> sendMessageQueue = null)
        {
            Url = null;

            _logger = logger;
            _connectionFactory = (uri, token) => Task.FromResult(connectedSocket);

            _memoryPool = new MemoryPool(receivingMemoryConfig.InitialMemorySize, receivingMemoryConfig.MarginSize, logger);

            _sentMessageQueue = sendMessageQueue ?? Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            _sentMessageQueueReader = _sentMessageQueue.Reader;
            _sentMessageQueueWriter = _sentMessageQueue.Writer;
        }

        public WebSocket NativeSocket => _socket;
        public ClientWebSocket NativeClient => _socket as ClientWebSocket;

        public bool IsOpen => _socket != null && _socket.State == WebSocketState.Open;
        public bool IsClosed => _socket != null && _socket.State == WebSocketState.Closed;

        public WebSocketState WebSocketState => _socket?.State ?? WebSocketState.None;

        public IObservable<ResponseMessage> MessageReceived => _binaryMessageReceivedSubject
            .Select(ResponseMessage.BinaryMessage)
            .Merge(_textMessageReceivedSubject.Select(ResponseMessage.TextMessage));

        public IObservable<byte[]> BinaryMessageReceived => _binaryMessageReceivedSubject.AsObservable();

        public IObservable<byte[]> RawTextMessageReceived => _textMessageReceivedSubject.AsObservable();

        public IObservable<string> TextMessageReceived =>
            RawTextMessageReceived.Select(MessageEncoding.GetString);

        public IObservable<CloseMessage> CloseMessageReceived => _closeMessageReceivedSubject.AsObservable();

        public IObservable<WebSocketExceptionDetail> ExceptionHappened => _exceptionSubject.AsObservable();

        /// <summary>
        /// Start connect and listening to the websocket stream on the background thread
        /// </summary>
        public async Task ConnectAndStartListening()
        {
            using (await _openLocker.LockAsync().ConfigureAwait(false))
            {
                if (IsOpen)
                {
                    _logger?.Warn(FormatLogMessage("BinaryWebSocketClient is already open."));
                    return;
                }

                if (IsDisposed)
                {
                    _logger?.Error(FormatLogMessage("BinaryWebSocketClient is already disposed."));
                    throw new ObjectDisposedException("BinaryWebSocketClient is already disposed.");
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
                }
                _socket = await _connectionFactory(uri, token).ConfigureAwait(false);
                _logger?.Log(FormatLogMessage("Start Listening..."));
                WaitUntilClose = Listen(_socket, token);
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

                _sentMessageQueueWriter.Complete();

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
        /// Close WebSocket
        /// </summary>
        /// <param name="status"></param>
        /// <param name="statusDescription"></param>
        /// <returns>
        /// true is normal.
        /// If false, there is a problem.
        /// This function is equivalent to CloseAsync(status, statusDescription, true)
        /// </returns>
        public Task CloseAsync(WebSocketCloseStatus status, string statusDescription)
        {
            return CloseAsync(status, statusDescription, true);
        }

        /// <summary>
        /// Close WebSocket
        /// </summary>
        /// <param name="status"></param>
        /// <param name="statusDescription"></param>
        /// <param name="dispose"></param>
        /// <returns>true is normal.
        /// If false, there is a problem.
        /// </returns>
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
                    _logger?.Error(e, FormatLogMessage($"Error while stopping client, message: '{e.Message}'"));
                    throw;
                }
            }
        }

        private static Func<Uri, CancellationToken, Task<WebSocket>> MakeConnectionFactory(Func<ClientWebSocket> clientFactory)
        {
            if (clientFactory == null)
            {
                return null;
            }

            return (async (uri, token) =>
            {
                var client = clientFactory();
                await client.ConnectAsync(uri, token).ConfigureAwait(false);
                return client;
            });
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