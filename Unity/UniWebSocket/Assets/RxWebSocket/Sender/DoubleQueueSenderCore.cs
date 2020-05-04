﻿using System;
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

#if NETSTANDARD2_1 || NETSTANDARD2_0
using System.Reactive.Subjects;
using System.Reactive.Linq;
#else
using UniRx;
#endif

namespace RxWebSocket
{
    internal class DoubleQueueSenderCore : IWebSocketMessageSenderCore
    {
        private readonly Channel<ArraySegment<byte>> _binaryMessageQueue;
        private readonly ChannelReader<ArraySegment<byte>> _binaryMessageQueueReader;
        private readonly ChannelWriter<ArraySegment<byte>> _binaryMessageQueueWriter;

        private readonly Channel<ArraySegment<byte>> _textMessageQueue;
        private readonly ChannelReader<ArraySegment<byte>> _textMessageQueueReader;
        private readonly ChannelWriter<ArraySegment<byte>> _textMessageQueueWriter;

        private readonly AsyncLock _sendLocker = new AsyncLock();
        private readonly Subject<WebSocketExceptionDetail> _exceptionSubject = new Subject<WebSocketExceptionDetail>();
        private readonly CancellationTokenSource _stopCancellationTokenSource = new CancellationTokenSource();

        private WebSocket _socket;
        private ILogger _logger;
        private bool _stop;

        public Encoding MessageEncoding { get; } = Encoding.UTF8;
        public bool IsDisposed { get; private set; }
        public string Name { get; private set; } = "CLIENT";

        public bool IsOpen => _socket != null && _socket.State == WebSocketState.Open;

        public IObservable<WebSocketExceptionDetail> ExceptionHappenedInSending => _exceptionSubject.AsObservable();

        public DoubleQueueSenderCore(
            Channel<ArraySegment<byte>> binaryMessageQueue = null,
            Channel<ArraySegment<byte>> textMessageQueue = null)
        {
            _binaryMessageQueue = binaryMessageQueue ?? Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            _binaryMessageQueueReader = _binaryMessageQueue.Reader;
            _binaryMessageQueueWriter = _binaryMessageQueue.Writer;

            _textMessageQueue = textMessageQueue ?? Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            _textMessageQueueReader = _textMessageQueue.Reader;
            _textMessageQueueWriter = _textMessageQueue.Writer;
        }

        public void SetLoggingConfig(ILogger logger, string name)
        {
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
                _stop = true;
                _stopCancellationTokenSource.Cancel();
            }
        }

        public void SendMessageFromQueue()
        {
            _ = Task.Factory.StartNew(_ => SendBinaryMessageFromQueue(), TaskCreationOptions.LongRunning, _stopCancellationTokenSource.Token);
            _ = Task.Factory.StartNew(_ => SendTextMessageFromQueue(), TaskCreationOptions.LongRunning, _stopCancellationTokenSource.Token);
        }

        public bool Send(string message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return _textMessageQueueWriter.TryWrite(new ArraySegment<byte>(MessageEncoding.GetBytes(message)));
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
                return _binaryMessageQueueWriter.TryWrite(new ArraySegment<byte>(message));
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
                return _binaryMessageQueueWriter.TryWrite(message);
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the Send function is 0 Count. Please correct it.");
            }
        }

        public bool Send(byte[] message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(message))
            {

                if (messageType == WebSocketMessageType.Binary)
                {
                    return _binaryMessageQueueWriter.TryWrite(new ArraySegment<byte>(message));
                }
                else
                {
                    return _textMessageQueueWriter.TryWrite(new ArraySegment<byte>(message));
                }
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is null or 0 Length. Please correct it.");
            }
        }

        public bool Send(ref ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {

                if (messageType == WebSocketMessageType.Binary)
                {
                    return _binaryMessageQueueWriter.TryWrite(message);
                }
                else
                {
                    return _textMessageQueueWriter.TryWrite(message);
                }
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
                return SendTextInternalSynchronized(new ArraySegment<byte>(MessageEncoding.GetBytes(message)));
            }

            throw new WebSocketBadInputException($"Input message (string) of the SendInstant function is null or empty. Please correct it.");
        }

        public Task SendInstant(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendBinaryInternalSynchronized(new ArraySegment<byte>(message));
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        public Task SendInstant(byte[] message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(message))
            {

                if (messageType == WebSocketMessageType.Binary)
                {
                    return SendBinaryInternalSynchronized(new ArraySegment<byte>(message));
                }
                else
                {
                    return SendTextInternalSynchronized(new ArraySegment<byte>(message));
                }
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        public Task SendInstant(ref ArraySegment<byte> message)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return SendBinaryInternalSynchronized(message);
            }

            throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is 0 Count. Please correct it.");
        }

        public Task SendInstant(ref ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {

                if (messageType == WebSocketMessageType.Binary)
                {
                    return SendBinaryInternalSynchronized(message);
                }
                else
                {
                    return SendTextInternalSynchronized(message);
                }
            }

            throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is null or 0 Length. Please correct it.");
        }


        private async Task SendBinaryMessageFromQueue()
        {
            try
            {
                while (await _binaryMessageQueueReader.WaitToReadAsync(_stopCancellationTokenSource.Token).ConfigureAwait(false))
                {
                    while (!_stop && _binaryMessageQueueReader.TryRead(out var message))
                    {
                        try
                        {
                            await SendBinaryInternalSynchronized(message).ConfigureAwait(false);
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
                if (_stopCancellationTokenSource.IsCancellationRequested || IsDisposed)
                {
                    // disposing/canceling, do nothing and exit
                    return;
                }

                _logger?.Error(e, FormatLogMessage($"Sending message thread failed, error: {e.Message}."));
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.SendQueue));
            }
        }

        private async Task SendTextMessageFromQueue()
        {
            try
            {
                while (await _textMessageQueueReader.WaitToReadAsync(_stopCancellationTokenSource.Token).ConfigureAwait(false))
                {
                    while (!_stop && _textMessageQueueReader.TryRead(out var message))
                    {
                        try
                        {
                            await SendTextInternalSynchronized(message).ConfigureAwait(false);
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
                if (_stopCancellationTokenSource.IsCancellationRequested || IsDisposed)
                {
                    // disposing/canceling, do nothing and exit
                    return;
                }

                _logger?.Error(e, FormatLogMessage($"Sending message thread failed, error: {e.Message}."));
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.SendQueue));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task SendTextInternalSynchronized(ArraySegment<byte> message)
        {
            using (await _sendLocker.LockAsync().ConfigureAwait(false))
            {
                if (_stop)
                {
                    return;
                }

                if (!IsOpen)
                {
                    _logger?.Warn(FormatLogMessage($"Client is not connected to server, cannot send:  {message}"));
                    return;
                }

                _logger?.Log(FormatLogMessage($"Sending: Type Text, length {message.Count}"));

                await _socket
                    .SendAsync(message, WebSocketMessageType.Text, true, _stopCancellationTokenSource.Token)
                    .ConfigureAwait(false);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task SendBinaryInternalSynchronized(ArraySegment<byte> message)
        {
            using (await _sendLocker.LockAsync().ConfigureAwait(false))
            {
                if (_stop)
                {
                    return;
                }

                if (!IsOpen)
                {
                    _logger?.Warn(FormatLogMessage($"Client is not connected to server, cannot send:  {message}"));
                    return;
                }

                _logger?.Log(FormatLogMessage($"Sending: Type Binary, length {message.Count}"));

                await _socket
                    .SendAsync(message, WebSocketMessageType.Binary, true, _stopCancellationTokenSource.Token)
                    .ConfigureAwait(false);
            }
        }

        private string FormatLogMessage(string msg)
        {
            return $"[WEBSOCKET {Name}] {msg}";
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                _stop = true;

                _stopCancellationTokenSource.Cancel();

                _binaryMessageQueueWriter.Complete();
                _textMessageQueueWriter.Complete();
                _exceptionSubject.Dispose();
                _stopCancellationTokenSource.Dispose();
            }
        }
    }
}