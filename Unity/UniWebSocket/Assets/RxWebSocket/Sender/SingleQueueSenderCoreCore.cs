using System;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using RxWebSocket.Exceptions;
using RxWebSocket.Logging;
using RxWebSocket.Threading;
using RxWebSocket.Validations;
using UniRx;

namespace RxWebSocket
{
    public class SingleQueueSenderCoreCore : IWebSocketMessageSenderCore
    {
        private readonly Channel<SentMessage> _sentMessageQueue;
        private readonly ChannelReader<SentMessage> _sentMessageQueueReader;
        private readonly ChannelWriter<SentMessage> _sentMessageQueueWriter;
        private readonly AsyncLock _sendLocker = new AsyncLock();
        private readonly Subject<WebSocketExceptionDetail> _exceptionSubject = new Subject<WebSocketExceptionDetail>();

        private WebSocket _socket;
        private CancellationToken _sendingCancellationToken, _waitQueueCancellationToken;
        private ILogger _logger;

        public Encoding MessageEncoding { get; } = Encoding.UTF8;
        public bool IsDisposed { get; private set; }
        public string Name { get; internal set; } = "CLIENT";

        public bool IsOpen => _socket != null && _socket.State == WebSocketState.Open;

        public IObservable<WebSocketExceptionDetail> ExceptionHappenedInSending => _exceptionSubject.AsObservable();

        public SingleQueueSenderCoreCore(Channel<SentMessage> sentMessageQueue = null)
        {
            _sentMessageQueue = sentMessageQueue ?? Channel.CreateUnbounded<SentMessage>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            _sentMessageQueueReader = _sentMessageQueue.Reader;
            _sentMessageQueueWriter = _sentMessageQueue.Writer;
        }

        public void SetInternal(
            CancellationToken sendingCancellationToken, 
            CancellationToken waitQueueCancellationToken,
            ILogger logger)
        {
            _sendingCancellationToken = sendingCancellationToken;
            _waitQueueCancellationToken = waitQueueCancellationToken;
            _logger = logger;
        }

        public void SetSocket(WebSocket webSocket)
        {
            _socket = webSocket;
        }

        public bool Send(string message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return _sentMessageQueueWriter.TryWrite(new SentMessage(new ArraySegment<byte>(MessageEncoding.GetBytes(message)), WebSocketMessageType.Text));
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

        public bool Send(ArraySegment<byte> message)
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

        public bool Send(ArraySegment<byte> message, WebSocketMessageType messageType)
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
                return SendInternalSynchronized(new SentMessage(new ArraySegment<byte>(MessageEncoding.GetBytes(message)), WebSocketMessageType.Text));
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

        public Task SendInstant(ArraySegment<byte> message)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return SendInternalSynchronized(new SentMessage(message, WebSocketMessageType.Binary));
            }

            throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is 0 Count. Please correct it.");

        }

        public Task SendInstant(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return SendInternalSynchronized(new SentMessage(message, messageType));
            }

            throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is null or 0 Length. Please correct it.");

        }

        public async Task SendMessageFromQueue()
        {
            try
            {
                while (await _sentMessageQueueReader.WaitToReadAsync(_waitQueueCancellationToken).ConfigureAwait(false))
                {
                    while (_sentMessageQueueReader.TryRead(out var message))
                    {

                        try
                        {
                            await SendInternalSynchronized(message).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            _logger?.Error(e, FormatLogMessage($"Failed to send binary message: '{message}'. Error: {e.Message}"));
                            _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.Send));
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
                if (_waitQueueCancellationToken.IsCancellationRequested || IsDisposed)
                {
                    // disposing/canceling, do nothing and exit
                    return;
                }

                _logger?.Error(e, FormatLogMessage($"Sending message thread failed, error: {e.Message}."));
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.SendQueue));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task SendInternalSynchronized(SentMessage message)
        {
            using (await _sendLocker.LockAsync().ConfigureAwait(false))
            {
                if (!IsOpen)
                {
                    _logger?.Warn(FormatLogMessage($"Client is not connected to server, cannot send:  {message}"));
                    return;
                }

                _logger?.Log(FormatLogMessage($"Sending:  {message}"));

                await _socket
                    .SendAsync(message.Bytes, message.MessageType, true, _sendingCancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
            _sentMessageQueueWriter.Complete();
            _exceptionSubject.Dispose();
        }

        private string FormatLogMessage(string msg)
        {
            return $"[WEBSOCKET {Name}] {msg}";
        }
    }
}