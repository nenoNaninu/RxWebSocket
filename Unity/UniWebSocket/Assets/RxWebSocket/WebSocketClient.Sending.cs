using System;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RxWebSocket.Exceptions;
using RxWebSocket.Validations;

namespace RxWebSocket
{
    public partial class WebSocketClient
    {
        public bool Send(string message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return _sendMessageQueueWriter.TryWrite(new SendMessage(new ArraySegment<byte>(MessageEncoding.GetBytes(message)), WebSocketMessageType.Text));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (string) of the Send function is null or empty. Please correct it.");
            }
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        public bool Send(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return _sendMessageQueueWriter.TryWrite(new SendMessage(new ArraySegment<byte>(message), WebSocketMessageType.Binary));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is null or 0 Length. Please correct it.");
            }
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        public bool Send(ArraySegment<byte> message)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return _sendMessageQueueWriter.TryWrite(new SendMessage(message, WebSocketMessageType.Binary));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the Send function is 0 Count. Please correct it.");
            }
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        /// <param name="messageType"></param>
        public bool Send(byte[] message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return _sendMessageQueueWriter.TryWrite(new SendMessage(new ArraySegment<byte>(message), messageType));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is null or 0 Length. Please correct it.");
            }
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        /// <param name="messageType"></param>
        public bool Send(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return _sendMessageQueueWriter.TryWrite(new SendMessage(message, messageType));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the Send function is 0 Count. Please correct it.");
            }
        }

        /// <summary>
        /// Send text message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(string message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(new SendMessage(new ArraySegment<byte>(MessageEncoding.GetBytes(message)), WebSocketMessageType.Text));
            }

            throw new WebSocketBadInputException($"Input message (string) of the SendInstant function is null or empty. Please correct it.");
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(new SendMessage(new ArraySegment<byte>(message), WebSocketMessageType.Binary));
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        public Task SendInstant(byte[] message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(new SendMessage(new ArraySegment<byte>(message), messageType));
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(ArraySegment<byte> message)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return SendInternalSynchronized(new SendMessage(message, WebSocketMessageType.Binary));
            }

            throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is 0 Count. Please correct it.");
        }

        public Task SendInstant(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(ref message))
            {
                return SendInternalSynchronized(new SendMessage(message, messageType));
            }

            throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        private async Task SendMessageFromQueue()
        {
            try
            {
                while (await _sendMessageQueueReader.WaitToReadAsync(_cancellationAllJobs.Token).ConfigureAwait(false))
                {
                    while (_sendMessageQueueReader.TryRead(out var message))
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
                if (_cancellationAllJobs.IsCancellationRequested || IsDisposed)
                {
                    // disposing/canceling, do nothing and exit
                    return;
                }

                _logger?.Error(e, FormatLogMessage($"Sending message thread failed, error: {e.Message}."));
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.SendQueue));
            }
        }

        private void StartBackgroundThreadForSendingMessage()
        {
#pragma warning disable 4014
            Task.Factory.StartNew(_ => SendMessageFromQueue(), TaskCreationOptions.LongRunning, _cancellationAllJobs.Token);
#pragma warning restore 4014
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task SendInternalSynchronized(SendMessage message)
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
                    .SendAsync(message.Bytes, message.MessageType, true, _cancellationCurrentJobs.Token)
                    .ConfigureAwait(false);
            }
        }
    }
}