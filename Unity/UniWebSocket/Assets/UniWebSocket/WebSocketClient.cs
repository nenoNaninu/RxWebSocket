using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using UniWebSocket.Threading;

namespace UniWebSocket
{
    public partial class WebSocketClient : IWebSocketClient
    {
        private readonly ILogger _logger;

        private readonly AsyncLock _locker = new AsyncLock();
        private Uri _url;
        private readonly Func<Uri, CancellationToken, Task<WebSocket>> _connectionFactory;

        private bool _disposing;

        private readonly byte[] _memoryPool = new byte[1024 * 512];

        private WebSocket _client;
        private CancellationTokenSource _cancellationCurrentJobs;
        private CancellationTokenSource _cancellationAllJobs;
        private Encoding _messageEncoding;

        private readonly Subject<ResponseMessage> _messageReceivedSubject = new Subject<ResponseMessage>();
        private readonly Subject<WebSocketCloseStatus> _disconnectedSubject = new Subject<WebSocketCloseStatus>();
        private readonly Subject<WebSocketExceptionDetail> _exceptionSubject = new Subject<WebSocketExceptionDetail>();

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="maxReceivedMessageSize">Maximum array(byte[]) length of received data. default is 512*1024 byte(512KB)</param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, int maxReceivedMessageSize, ILogger logger = null, Func<ClientWebSocket> clientFactory = null)
            : this(url, GetConnectedClientFactory(clientFactory))
        {
            _logger = logger;
            _memoryPool = new byte[maxReceivedMessageSize];
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, ILogger logger, Func<ClientWebSocket> clientFactory = null)
            : this(url, GetConnectedClientFactory(clientFactory))
        {
            _logger = logger;
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, Func<ClientWebSocket> clientFactory = null)
            : this(url, GetConnectedClientFactory(clientFactory))
        {
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="connectionFactory">Optional factory for native creating and connecting to a websocket. The method should return a <see cref="WebSocket"/> which is connected. Use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
        {
            Validations.ValidationUtils.ValidateInput(url, nameof(url));

            _url = url;
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

        public Uri Url
        {
            get => _url;
            set
            {
                Validations.ValidationUtils.ValidateInput(value, nameof(Url));
                _url = value;
            }
        }

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

        public IObservable<WebSocketExceptionDetail> ErrorHappened => _exceptionSubject.AsObservable();

        /// <summary>
        /// Get or set the name of the current websocket client instance.
        /// For logging purpose (in case you use more parallel websocket clients and want to distinguish between them)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Returns true if ConnectAndStartListening() method was called at least once. False if not started or disposed
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Returns true if client is running and connected to the server
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <inheritdoc />
        public Encoding MessageEncoding
        {
            get => _messageEncoding ?? Encoding.UTF8;
            set => _messageEncoding = value;
        }

        /// <inheritdoc />
        public ClientWebSocket NativeClient => GetNativeClient(_client);

        /// <summary>
        /// Last time the message was received.
        /// </summary>
        public DateTime LastReceivedTime { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// Start listening to the websocket stream on the background thread
        /// </summary>
        public async Task ConnectAndStartListening()
        {
            if (IsStarted)
            {
                _logger?.Log(FormatLogMessage("Client already started, ignoring.."));
                return;
            }

            IsStarted = true;

            _logger?.Log(FormatLogMessage("Starting.."));
            _cancellationCurrentJobs = new CancellationTokenSource();
            _cancellationAllJobs = new CancellationTokenSource();

            await StartClient(_url, _cancellationCurrentJobs.Token).ConfigureAwait(false);

            StartBackgroundThreadForSendingText();
            StartBackgroundThreadForSendingBinary();
        }

        /// <summary>
        /// Terminate the websocket connection and cleanup everything
        /// </summary>
        public void Dispose()
        {
            _disposing = true;
            _logger?.Log(FormatLogMessage("Disposing.."));
            try
            {
                _cancellationAllJobs?.Cancel();
                _cancellationCurrentJobs?.Cancel();

                _client?.Abort();
                _client?.Dispose();

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

            IsRunning = false;
            IsStarted = false;
        }

        /// <summary>
        /// Close WebSocket
        /// </summary>
        /// <param name="status"></param>
        /// <param name="statusDescription"></param>
        /// <param name="dispose"></param>
        /// <returns></returns>
        public async Task<bool> CloseAsync(WebSocketCloseStatus status, string statusDescription, bool dispose = true)
        {
            if (_client == null || IsRunning == false)
            {
                IsStarted = false;
                IsRunning = false;
                return false;
            }

            try
            {
                await _client.CloseAsync(status, statusDescription, _cancellationCurrentJobs?.Token ?? CancellationToken.None).ConfigureAwait(false);

                if (dispose)
                {
                    this.Dispose();
                }
            }
            catch (Exception e)
            {
                _logger?.Error(FormatLogMessage($"Error while stopping client, message: '{e.Message}'"));
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.Close));
            }

            IsStarted = false;
            IsRunning = false;
            return true;
        }

        private static Func<Uri, CancellationToken, Task<WebSocket>> GetConnectedClientFactory(Func<ClientWebSocket> clientFactory)
        {
            if (clientFactory == null)
                return null;

            return (async (uri, token) =>
            {
                var client = clientFactory();
                await client.ConnectAsync(uri, token).ConfigureAwait(false);
                return client;
            });
        }

        private async Task StartClient(Uri uri, CancellationToken token)
        {
            try
            {
                _client = await _connectionFactory(uri, token).ConfigureAwait(false);
                IsRunning = true;
#pragma warning disable 4014
                Listen(_client, token);
#pragma warning restore 4014
                LastReceivedTime = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                _logger?.Error(e, FormatLogMessage($"Exception while connecting. detail: {e.Message}"));
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.Start));
            }
        }

        private bool IsClientConnected()
        {
            return _client.State == WebSocketState.Open;
        }

        private async Task Listen(WebSocket client, CancellationToken token)
        {
            try
            {
                do
                {
                    var arraySegment = new ArraySegment<byte>(_memoryPool);
                    int offsetCount = 0;

                    WebSocketReceiveResult result;
                    do
                    {
                        result = await client.ReceiveAsync(arraySegment, token).ConfigureAwait(false);
                        if (result.MessageType != WebSocketMessageType.Close)
                        {
                            offsetCount += result.Count;
                            arraySegment = new ArraySegment<byte>(_memoryPool, offsetCount, _memoryPool.Length - offsetCount);
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

                    var dstArray = new byte[offsetCount];
                    Array.Copy(_memoryPool, dstArray, offsetCount);

                    ResponseMessage message = result.MessageType == WebSocketMessageType.Text
                        ? ResponseMessage.TextMessage(MessageEncoding.GetString(dstArray))
                        : ResponseMessage.BinaryMessage(dstArray);

                    _logger?.Trace(FormatLogMessage($"Received:  {message.ToString()}"));
                    LastReceivedTime = DateTime.UtcNow;
                    _messageReceivedSubject.OnNext(message);
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

        private ClientWebSocket GetNativeClient(WebSocket client)
        {
            if (client == null)
                return null;
            var specific = client as ClientWebSocket;
            if (specific == null)
                throw new UniWebSocket.Exceptions.WebSocketException(
                    "Cannot cast 'WebSocket' client to 'ClientWebSocket', provide correct type via factory or don't use this property at all.");
            return specific;
        }

        private string FormatLogMessage(string msg)
        {
            var name = Name ?? "CLIENT";
            return $"[WEBSOCKET {name}] {msg}";
        }
    }
}