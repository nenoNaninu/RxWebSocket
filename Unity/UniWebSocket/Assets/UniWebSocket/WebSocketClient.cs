using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UniWebSocket.Exceptions;
using UniWebSocket.Threading;
using UniWebSocket.Validations;

namespace UniWebSocket
{
    public partial class WebSocketClient : IWebSocketClient
    {
        #region Member variable with state.

        private readonly ILogger _logger;

        private readonly AsyncLock _locker = new AsyncLock();
        private readonly Func<Uri, CancellationToken, Task<WebSocket>> _connectionFactory;

        private readonly MemoryPool _memoryPool;

        private readonly Subject<ResponseMessage> _messageReceivedSubject = new Subject<ResponseMessage>();
        private readonly Subject<WebSocketCloseStatus> _disconnectedSubject = new Subject<WebSocketCloseStatus>();
        private readonly Subject<WebSocketExceptionDetail> _exceptionSubject = new Subject<WebSocketExceptionDetail>();

        private readonly BlockingCollection<string> _messagesTextToSendQueue = new BlockingCollection<string>();
        private readonly BlockingCollection<byte[]> _messagesBinaryToSendQueue = new BlockingCollection<byte[]>();

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
        /// Returns False if ConnectAndStartListening is not called or already called Dispose().
        /// </summary>
        public bool IsStarted { get; private set; }

        public bool IsDisposed { get; private set; }

        public Encoding MessageEncoding { get; set; } = Encoding.UTF8;

        public DateTime LastReceivedTime { get; private set; } = DateTime.UtcNow;

        #endregion

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
        public WebSocketClient(Uri url, int initialMemorySize, int receiveBufferSize, ILogger logger = null,
            Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectionFactory(clientFactory))
        {
            _logger = logger;
            _memoryPool = new MemoryPool(initialMemorySize, receiveBufferSize, logger);
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="initialMemorySize">
        /// initial memory pool size for receive. default is 64 * 1024 byte(64KB)
        /// if lack of memory, memory pool is increase so allocation occur. </param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, int initialMemorySize, ILogger logger = null, Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectionFactory(clientFactory))
        {
            _logger = logger;
            _memoryPool = new MemoryPool(initialMemorySize, 4 * 1024, logger);
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, ILogger logger, Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectionFactory(clientFactory))
        {
            _logger = logger;
            _memoryPool = new MemoryPool(64 * 1024, 4 * 1024, logger);
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectionFactory(clientFactory))
        {
            _memoryPool = new MemoryPool(64 * 1024, 4 * 1024);
        }

        private WebSocketClient(Uri url, Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
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
        public WebSocketClient(Uri url, int initialMemorySize, ILogger logger, Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
        {
            if (!ValidationUtils.ValidateInput(url))
            {
                throw new WebSocketBadInputException($"url is null. Please correct it.");
            }

            if (!ValidationUtils.ValidateInput(connectionFactory))
            {
                throw new WebSocketBadInputException($"connectionFactory is null. Please correct it.");
            }

            _logger = logger;
            _memoryPool = new MemoryPool(initialMemorySize, 4 * 1024, logger);

            Url = url;
            _connectionFactory = connectionFactory;
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
        public WebSocketClient(Uri url, int initialMemorySize, int receiveBufferSize, ILogger logger,
            Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
        {
            if (!ValidationUtils.ValidateInput(url))
            {
                throw new WebSocketBadInputException($"url is null. Please correct it.");
            }

            if (!ValidationUtils.ValidateInput(connectionFactory))
            {
                throw new WebSocketBadInputException($"connectionFactory is null. Please correct it.");
            }

            _logger = logger;
            _memoryPool = new MemoryPool(initialMemorySize, receiveBufferSize, logger);

            Url = url;
            _connectionFactory = connectionFactory;
        }

        public WebSocket NativeSocket => _socket;
        public ClientWebSocket NativeClient => _socket as ClientWebSocket;

        public bool IsConnected => _socket != null && _socket.State == WebSocketState.Open;
        public bool IsClosed => _socket != null && _socket.State == WebSocketState.Closed;

        public WebSocketState WebSocketState => _socket?.State ?? WebSocketState.None;

        public IObservable<ResponseMessage> MessageReceived => _messageReceivedSubject.AsObservable();

        public IObservable<byte[]> BinaryMessageReceived => _messageReceivedSubject.AsObservable()
            .Where(x => x.MessageType == WebSocketMessageType.Binary)
            .Select(x => x.Binary)
            .Publish().RefCount();

        public IObservable<string> TextMessageReceived => _messageReceivedSubject.AsObservable()
            .Where(x => x.MessageType == WebSocketMessageType.Text)
            .Select(x => x.Text)
            .Publish().RefCount();

        /// <summary>
        /// Stream for disconnection event (triggered after the connection was lost) 
        /// </summary>
        public IObservable<WebSocketCloseStatus> DisconnectionHappened => _disconnectedSubject.AsObservable();

        public IObservable<WebSocketExceptionDetail> ExceptionHappened => _exceptionSubject.AsObservable();

        /// <summary>
        /// Start connect and listening to the websocket stream on the background thread
        /// </summary>
        /// <returns>return true if successful</returns>
        public async Task<bool> ConnectAndStartListening()
        {
            if (IsStarted)
            {
                _logger?.Log(FormatLogMessage("Client already started, ignoring.."));
                return false;
            }

            if (IsDisposed)
            {
                _logger?.Log(FormatLogMessage("Client already disposed, ignoring.."));
                return false;
            }

            IsStarted = true;

            _logger?.Log(FormatLogMessage("Starting..."));
            var connectionTask = ConnectAndStartListeningInternal(Url, _cancellationCurrentJobs.Token).ConfigureAwait(false);

            StartBackgroundThreadForSendingText();
            StartBackgroundThreadForSendingBinary();

            return await connectionTask;
        }

        private async Task<bool> ConnectAndStartListeningInternal(Uri uri, CancellationToken token)
        {
            try
            {
                _socket = await _connectionFactory(uri, token).ConfigureAwait(false);
#pragma warning disable 4014
                Listen(_socket, token);
#pragma warning restore 4014
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
                _cancellationAllJobs?.Cancel();
                _cancellationCurrentJobs?.Cancel();

                _socket?.Abort();
                _socket?.Dispose();

                _cancellationAllJobs?.Dispose();
                _cancellationCurrentJobs?.Dispose();
                _messagesTextToSendQueue?.Dispose();
                _messagesBinaryToSendQueue?.Dispose();

                _messageReceivedSubject?.Dispose();
                _disconnectedSubject?.Dispose();
                _exceptionSubject?.Dispose();
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
        /// <param name="dispose"></param>
        /// <returns>true is normal.
        /// If false, there is a problem and even if dispose = true, it will not be automatically disposed.
        /// </returns>
        public async Task<bool> CloseAsync(WebSocketCloseStatus status, string statusDescription, bool dispose = true)
        {
            if (_socket == null || IsConnected == false)
            {
                IsStarted = false;
                return false;
            }

            try
            {
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
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        if (result.CloseStatus != null)
                        {
                            _disconnectedSubject.OnNext(result.CloseStatus.Value);
                        }

                        return;
                    }

                    var dstArray = _memoryPool.ToArray();

                    ResponseMessage message = result.MessageType == WebSocketMessageType.Text
                        ? ResponseMessage.TextMessage(MessageEncoding.GetString(dstArray))
                        : ResponseMessage.BinaryMessage(dstArray);

                    _logger?.Log(FormatLogMessage($"Received:  {message.ToString()}"));
                    LastReceivedTime = DateTime.UtcNow;

                    _messageReceivedSubject.OnNext(message);
                    //
                } while (client.State == WebSocketState.Open && !token.IsCancellationRequested);
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