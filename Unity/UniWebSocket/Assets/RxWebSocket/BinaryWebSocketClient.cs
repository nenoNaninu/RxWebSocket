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

        private readonly AsyncLock _sendLocker = new AsyncLock();
        private readonly AsyncLock _closeLocker = new AsyncLock();

        private readonly Subject<byte[]> _binaryMessageReceivedSubject = new Subject<byte[]>();
        private readonly Subject<string> _textMessageReceivedSubject = new Subject<string>();

        private readonly Subject<CloseMessage> _closeMessageReceivedSubject = new Subject<CloseMessage>();
        private readonly Subject<WebSocketExceptionDetail> _exceptionSubject = new Subject<WebSocketExceptionDetail>();

        private readonly BlockingCollection<ArraySegment<byte>> _sendMessageQueue = new BlockingCollection<ArraySegment<byte>>();

        private readonly CancellationTokenSource _cancellationCurrentJobs = new CancellationTokenSource();
        private readonly CancellationTokenSource _cancellationAllJobs = new CancellationTokenSource();

        private WebSocket _socket;

        public Uri Url { get; }

        /// <summary>
        /// For logging purpose.
        /// </summary>
        public string Name { get; set; } = "CLIENT";

        /// <summary>
        /// Returns true if ConnectAndStartListening() method was already called.
        /// Returns False if ConnectAndStartListening() is not called or already called Dispose().
        /// </summary>
        public bool IsStarted { get; private set; }

        public bool IsDisposed { get; private set; }

        public Encoding MessageEncoding { get; set; } = Encoding.UTF8;

        public DateTime LastReceivedTime { get; private set; } = DateTime.UtcNow;

        public Task Wait { get; private set; }
        #endregion

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public BinaryWebSocketClient(Uri url, Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectionFactory(clientFactory))
        {
            _memoryPool = new MemoryPool(64 * 1024, 4 * 1024);
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public BinaryWebSocketClient(Uri url, ILogger logger, Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectionFactory(clientFactory))
        {
            _logger = logger;
            _memoryPool = new MemoryPool(64 * 1024, 4 * 1024, logger);
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="initialMemorySize">
        /// initial memory pool size for receive. default is 64 * 1024 byte(64KB)
        /// if lack of memory, memory pool is increase so allocation occur. </param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public BinaryWebSocketClient(Uri url, int initialMemorySize, ILogger logger = null, Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectionFactory(clientFactory))
        {
            _logger = logger;
            _memoryPool = new MemoryPool(initialMemorySize, 4 * 1024, logger);
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="initialMemorySize">
        /// initial memory pool size for receive. default is 64 * 1024 byte(64KB)
        /// if lack of memory, memory pool is increase so allocation occur. </param>
        /// <param name="receiveBufferSize">
        /// if use ClientWebSocketOptions.SetBuffer(int receiveBufferSize, int sendBufferSize) in clientFactory, set this argument.
        /// default is 4 * 1024.
        /// </param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public BinaryWebSocketClient(Uri url, int initialMemorySize, int receiveBufferSize, ILogger logger = null, Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectionFactory(clientFactory))
        {
            _logger = logger;
            _memoryPool = new MemoryPool(initialMemorySize, receiveBufferSize, logger);
        }

        private BinaryWebSocketClient(Uri url, Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
        {
            if (!ValidationUtils.ValidateInput(url))
            {
                throw new WebSocketBadInputException($"url is null. Please correct it.");
            }

            Url = url;
            _connectionFactory = connectionFactory ?? (async (uri, token) =>
            {
                var client = new ClientWebSocket
                {
                    Options = {KeepAliveInterval = new TimeSpan(0, 0, 0, 10)}
                };
                await client.ConnectAsync(uri, token).ConfigureAwait(false);
                return client;
            });
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="initialMemorySize">
        /// initial memory pool size for receive. default is 64 * 1024 byte(64KB)
        /// if lack of memory, memory pool is increase so allocation occur. </param>
        /// <param name="logger"></param>
        /// <param name="connectionFactory">An optional factory for creating and connecting native Websockets. The method should return connected websocket.</param>
        public BinaryWebSocketClient(Uri url, int initialMemorySize, ILogger logger, Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
        {
            if (!ValidationUtils.ValidateInput(url))
            {
                throw new WebSocketBadInputException($"url is null. Please correct it.");
            }

            if (!ValidationUtils.ValidateInput(connectionFactory))
            {
                throw new WebSocketBadInputException($"connectionFactory is null. Please correct it.");
            }

            Url = url;

            _logger = logger;
            _connectionFactory = connectionFactory;
            _memoryPool = new MemoryPool(initialMemorySize, 4 * 1024, logger);
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="initialMemorySize">
        /// initial memory pool size for receive. default is 64 * 1024 byte(64KB)
        /// if lack of memory, memory pool is increase so allocation occur. </param>
        /// <param name="receiveBufferSize">
        /// if use ClientWebSocketOptions.SetBuffer(int receiveBufferSize, int sendBufferSize) in connectionFactory, set this argument.
        /// default is 4 * 1024.
        /// </param>
        /// <param name="logger"></param>
        /// <param name="connectionFactory">An optional factory for creating and connecting native Websockets. The method should return connected websocket.</param>
        public BinaryWebSocketClient(Uri url, int initialMemorySize, int receiveBufferSize, ILogger logger, Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
        {
            if (!ValidationUtils.ValidateInput(url))
            {
                throw new WebSocketBadInputException($"url is null. Please correct it.");
            }

            if (!ValidationUtils.ValidateInput(connectionFactory))
            {
                throw new WebSocketBadInputException($"connectionFactory is null. Please correct it.");
            }

            Url = url;

            _logger = logger;
            _connectionFactory = connectionFactory;
            _memoryPool = new MemoryPool(initialMemorySize, receiveBufferSize, logger);
        }

        /// <summary>
        /// For server(ASP.NET Core)
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="connectedSocket">Already connected socket.</param>
        public BinaryWebSocketClient(WebSocket connectedSocket, ILogger logger = null)
        {
            Url = null;

            _logger = logger;
            _connectionFactory = (uri, token) => Task.FromResult(connectedSocket);

            _memoryPool = new MemoryPool(64 * 1024, 4 * 1024, logger);
        }

        /// <summary>
        /// For server(ASP.NET Core)
        /// </summary>
        /// <param name="initialMemorySize"></param>
        /// <param name="logger"></param>
        /// <param name="connectedSocket">Already connected socket.</param>
        public BinaryWebSocketClient(WebSocket connectedSocket, int initialMemorySize, ILogger logger = null)
        {
            Url = null;

            _logger = logger;
            _connectionFactory = (uri, token) => Task.FromResult(connectedSocket);

            _memoryPool = new MemoryPool(initialMemorySize, 4 * 1024, logger);
        }

        /// <summary>
        /// For server(ASP.NET Core)
        /// </summary>
        /// <param name="initialMemorySize"></param>
        /// <param name="receiveBufferSize"></param>
        /// <param name="logger"></param>
        /// <param name="connectedSocket">Already connected socket.</param>
        public BinaryWebSocketClient(WebSocket connectedSocket, int initialMemorySize, int receiveBufferSize, ILogger logger = null)
        {
            Url = null;

            _logger = logger;
            _connectionFactory = (uri, token) => Task.FromResult(connectedSocket);

            _memoryPool = new MemoryPool(initialMemorySize, receiveBufferSize, logger);
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

        public IObservable<string> TextMessageReceived => _textMessageReceivedSubject.AsObservable();

        public IObservable<CloseMessage> CloseMessageReceived => _closeMessageReceivedSubject.AsObservable();

        public IObservable<WebSocketExceptionDetail> ExceptionHappened => _exceptionSubject.AsObservable();

        public int QueueCount => _sendMessageQueue.Count;

        /// <summary>
        /// Start connect and listening to the websocket stream on the background thread
        /// </summary>
        /// <returns>return true if successful</returns>
        public async Task<bool> ConnectAndStartListening()
        {
            if (IsStarted)
            {
                _logger?.Log(FormatLogMessage("Client already started, ignoring..."));
                return false;
            }

            if (IsDisposed)
            {
                _logger?.Log(FormatLogMessage("Client already disposed, ignoring..."));
                return false;
            }

            IsStarted = true;

            _logger?.Log(FormatLogMessage("Starting..."));
            var connectionTask = ConnectAndStartListeningInternal(Url, _cancellationCurrentJobs.Token);

            StartBackgroundThreadForSendingMessage();

            return await connectionTask.ConfigureAwait(false);
        }

        private async Task<bool> ConnectAndStartListeningInternal(Uri uri, CancellationToken token)
        {
            try
            {
                if (_socket == null)
                {
                    _logger?.Log(FormatLogMessage("Connecting..."));
                }
                _socket = await _connectionFactory(uri, token).ConfigureAwait(false);
                _logger?.Log(FormatLogMessage("Start Listening..."));
                Wait = Listen(_socket, token);
                LastReceivedTime = DateTime.UtcNow;
                return true;
            }
            catch (Exception e)
            {
                _logger?.Error(e, FormatLogMessage($"Exception while connecting. detail: {e.Message}"));
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.Start));
                return false;
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

                if (!this.IsClosed)
                {
                    _socket?.Abort();
                }

                _socket?.Dispose();

                _cancellationAllJobs.Dispose();
                _cancellationCurrentJobs.Dispose();
                _sendMessageQueue.Dispose();

                _binaryMessageReceivedSubject.Dispose();
                _textMessageReceivedSubject.Dispose();
                _closeMessageReceivedSubject.Dispose();
                _exceptionSubject.Dispose();
            }
            catch (Exception e)
            {
                _logger?.Error(e, FormatLogMessage($"Failed to dispose client, error: {e.Message}"));
                _exceptionSubject?.OnNext(new WebSocketExceptionDetail(e, ErrorType.Dispose));
            }
            finally
            {
                IsStarted = false;
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
        public Task<bool> CloseAsync(WebSocketCloseStatus status, string statusDescription)
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
        public async Task<bool> CloseAsync(WebSocketCloseStatus status, string statusDescription, bool dispose)
        {
            // prevent sending multiple disconnect requests.
            using (await _closeLocker.LockAsync().ConfigureAwait(false))
            {
                if (IsClosed)
                {
                    return true;
                }

                if (_socket == null ||
                    _socket.State == WebSocketState.Aborted ||
                    _socket.State == WebSocketState.None)
                {
                    _logger?.Error(FormatLogMessage($"Called CloseAsync, but websocket state is not correct."));
                    IsStarted = false;
                    return false;
                }

                try
                {
                    // await until the connection closed.
                    await _socket.CloseAsync(status, statusDescription, _cancellationCurrentJobs.Token).ConfigureAwait(false);

                    if (dispose)
                    {
                        this.Dispose();
                    }

                    return true;
                }
                catch (Exception e)
                {
                    _logger?.Error(e, FormatLogMessage($"Error while stopping client, message: '{e.Message}'"));
                    _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.Close));
                    return false;
                }
                finally
                {
                    IsStarted = false;
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
                        var receivedText = MessageEncoding.GetString(_memoryPool.ToArray());
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
                            await CloseAsync(WebSocketCloseStatus.NormalClosure, $"Response to the close message. Received close status: {result.CloseStatus.Value}", true).ConfigureAwait(false);
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
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.Listen));
            }
        }

        private string FormatLogMessage(string msg)
        {
            return $"[WEBSOCKET {Name}] {msg}";
        }
    }
}