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

        private readonly byte[] _memoryPool = new byte[1024 * 512];

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
        /// Returns true if ConnectAndStartListening() method was called at least once. False if not started or disposed
        /// </summary>
        public bool IsStarted { get; private set; }

        public bool IsDisposed { get; private set; }

        public Encoding MessageEncoding { get; set; } = Encoding.UTF8;

        public DateTime LastReceivedTime { get; private set; } = DateTime.UtcNow;

        #endregion

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="maxReceivedMessageSize">Maximum array(byte[]) length of received data. default is 512*1024 byte(512KB)</param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, int maxReceivedMessageSize, ILogger logger = null, Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectedClientFactory(clientFactory))
        {
            _logger = logger;
            _memoryPool = new byte[maxReceivedMessageSize];
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, ILogger logger, Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectedClientFactory(clientFactory))
        {
            _logger = logger;
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, Func<ClientWebSocket> clientFactory = null)
            : this(url, MakeConnectedClientFactory(clientFactory))
        {
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="connectionFactory">Optional factory for native creating and connecting to a websocket. The method should return a <see cref="WebSocket"/> which is connected. Use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
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

        public IObservable<WebSocketExceptionDetail> ErrorHappened => _exceptionSubject.AsObservable();

        /// <summary>
        /// Start listening to the websocket stream on the background thread
        /// </summary>
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

        private static Func<Uri, CancellationToken, Task<WebSocket>> MakeConnectedClientFactory(Func<ClientWebSocket> clientFactory)
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
                    Buffer.BlockCopy(_memoryPool, 0, dstArray, 0, offsetCount);

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