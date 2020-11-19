using System;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using RxWebSocket.Exceptions;
using RxWebSocket.Logging;
using RxWebSocket.Message;
using RxWebSocket.Threading;
using RxWebSocket.Utils;
using RxWebSocket.Validations;

#if NETSTANDARD2_1 || NETSTANDARD2_0|| NETCOREAPP
using System.Reactive.Subjects;
using System.Reactive.Linq;
#else
using UniRx;
#endif

namespace RxWebSocket.Senders
{
    internal class SingleQueueSenderCore : IWebSocketMessageSenderCore
    {
        private readonly Channel<SentMessage> _sentMessageQueue;
        private readonly ChannelReader<SentMessage> _sentMessageQueueReader;
        private readonly ChannelWriter<SentMessage> _sentMessageQueueWriter;

        private readonly AsyncLock _sendLocker = new AsyncLock();
        private readonly Subject<WebSocketBackgroundException> _exceptionSubject = new Subject<WebSocketBackgroundException>();
        private readonly CancellationTokenSource _stopCancellationTokenSource = new CancellationTokenSource();

        private WebSocket _socket;
        private ILogger _logger;
        private Encoding _messageEncoding;

        private int _isStopRequested = 0;
        private int _isDisposed = 0;

        public bool IsDisposed => 0 < _isDisposed;
        private bool IsOpen => _socket != null && _socket.State == WebSocketState.Open;

        public string Name { get; private set; } = "CLIENT";

        public IObservable<WebSocketBackgroundException> ExceptionHappenedInSending => _exceptionSubject.AsObservable();

        public SingleQueueSenderCore(Channel<SentMessage> sentMessageQueue = null)
        {
            _sentMessageQueue = sentMessageQueue ?? Channel.CreateUnbounded<SentMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            _sentMessageQueueReader = _sentMessageQueue.Reader;
            _sentMessageQueueWriter = _sentMessageQueue.Writer;
        }

        public void SetConfig(Encoding encoding, ILogger logger, string name)
        {
            _messageEncoding = encoding;
            _logger = logger;
            Name = name;
        }

        public void SetSocket(WebSocket webSocket)
        {
            _socket = webSocket;
        }

        public async Task StopAsync()
        {
            using (await _sendLocker.LockAsync().ConfigureAwait(false))
            {
                StopCore();
            }
        }

        private void StopCore()
        {
            if (Interlocked.Increment(ref _isStopRequested) == 1)
            {
                _stopCancellationTokenSource.CancelWithoutException();

                _sentMessageQueueWriter.TryComplete();
            }
        }

        public bool Send(string message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return _sentMessageQueueWriter.TryWrite(new SentMessage(new ArraySegment<byte>(_messageEncoding.GetBytes(message)), WebSocketMessageType.Text));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (string) of the Send function is null or empty. Please correct it.");
            }
        }

        public bool Send(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return _sentMessageQueueWriter.TryWrite(new SentMessage(new ArraySegment<byte>(message), WebSocketMessageType.Binary));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is null or 0 Length. Please correct it.");
            }
        }

        public bool Send(byte[] message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return _sentMessageQueueWriter.TryWrite(new SentMessage(new ArraySegment<byte>(message), messageType));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is null or 0 Length. Please correct it.");
            }
        }

        public bool Send(ref ArraySegment<byte> message)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return _sentMessageQueueWriter.TryWrite(new SentMessage(message, WebSocketMessageType.Binary));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the Send function is 0 Count. Please correct it.");
            }
        }

        public bool Send(ref ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return _sentMessageQueueWriter.TryWrite(new SentMessage(message, messageType));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the Send function is 0 Count. Please correct it.");
            }
        }

        public Task SendInstant(string message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(new SentMessage(new ArraySegment<byte>(_messageEncoding.GetBytes(message)), WebSocketMessageType.Text));
            }

            throw new WebSocketBadInputException($"Input message (string) of the SendInstant function is null or empty. Please correct it.");
        }

        public Task SendInstant(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(new SentMessage(new ArraySegment<byte>(message), WebSocketMessageType.Binary));
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        public Task SendInstant(byte[] message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(new SentMessage(new ArraySegment<byte>(message), messageType));
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        public Task SendInstant(ref ArraySegment<byte> message)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return SendInternalSynchronized(new SentMessage(message, WebSocketMessageType.Binary));
            }

            throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is 0 Count. Please correct it.");
        }

        public Task SendInstant(ref ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return SendInternalSynchronized(new SentMessage(message, messageType));
            }

            throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        public void StartSendingMessageFromQueue()
        {
            Task.Run(StartSendingMessageFromQueueInternal).Forget(_logger);
        }

        public async Task StartSendingMessageFromQueueInternal()
        {
            try
            {
                while (_isStopRequested == 0 && await _sentMessageQueueReader.WaitToReadAsync(_stopCancellationTokenSource.Token).ConfigureAwait(false))
                {
                    while (_isStopRequested == 0 && _sentMessageQueueReader.TryRead(out var message))
                    {
                        try
                        {
                            await SendInternalSynchronized(message).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            _logger?.Error(e, FormatLogMessage($"Failed to send binary message: '{message}'. Error: {e.Message}"));
                            _exceptionSubject.OnNext(new WebSocketBackgroundException(e, ExceptionType.Send));
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // task was canceled, ignore
            }
            catch (OperationCanceledException)
            {
                // operation was canceled, ignore
            }
            catch (Exception e)
            {
                if (_stopCancellationTokenSource.IsCancellationRequested || IsDisposed)
                {
                    // disposing/canceling, do nothing and exit
                    return;
                }

                _logger?.Error(e, FormatLogMessage($"Sending message thread failed, error: {e.Message}."));
                _exceptionSubject.OnNext(new WebSocketBackgroundException(e, ExceptionType.SendQueue));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task SendInternalSynchronized(SentMessage message)
        {
            using (await _sendLocker.LockAsync().ConfigureAwait(false))
            {
                if (_isStopRequested != 0)
                {
                    return;
                }

                if (!IsOpen)
                {
                    _logger?.Warn(FormatLogMessage($"Client is not connected to server, cannot send:  {message}"));
                    return;
                }

                _logger?.Log(FormatLogMessage($"Sending: Type {message.MessageType}, length {message.Bytes.Count}"));

                await _socket
                    .SendAsync(message.Bytes, message.MessageType, true, _stopCancellationTokenSource.Token)
                    .ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref _isDisposed) == 1)
            {
                using (_stopCancellationTokenSource)
                using (_exceptionSubject)
                {
                    StopCore();
                }
            }
        }

        private string FormatLogMessage(string msg)
        {
            return $"[WEBSOCKET {Name}] {msg}";
        }
    }
}