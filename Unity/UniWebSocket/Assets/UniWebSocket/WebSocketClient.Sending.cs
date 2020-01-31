using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using UniWebSocket.Exceptions;
using UniWebSocket.Validations;

namespace UniWebSocket
{
    public partial class WebSocketClient
    {
        /// <summary>
        /// Send text message to the websocket channel. 
        /// It inserts the message to the queue and actual sending is done on an other thread
        /// </summary>
        /// <param name="message">Text message to be sent</param>
        public void Send(string message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                _messagesTextToSendQueue.Add(message);
                return;
            }

            throw new WebSocketBadInputException($"Input message (string) of the Send function is null or empty. Please correct it.");
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It inserts the message to the queue and actual sending is done on an other thread
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        public void Send(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                _messagesBinaryToSendQueue.Add(message);
                return;
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is null or 0 Length. Please correct it.");
        }

        /// <summary>
        /// Send text message to the websocket channel. 
        /// It doesn't use a sending queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(string message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(message);
            }

            throw new WebSocketBadInputException($"Input message (string) of the SendInstant function is null or empty. Please correct it.");
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a sending queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(message);
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        private async Task SendTextFromQueue()
        {
            try
            {
                foreach (var message in _messagesTextToSendQueue.GetConsumingEnumerable(_cancellationAllJobs.Token))
                {
                    try
                    {
                        await SendInternalSynchronized(message).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        _logger?.Error(e, FormatLogMessage($"Failed to send text message: '{message}'. Error: {e.Message}"));
                        _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.SendText));
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

                _logger?.Error(e, FormatLogMessage($"Sending text thread failed, error: {e.Message}."));
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.TextQueue));
            }
        }

        private async Task SendBinaryFromQueue()
        {
            try
            {
                foreach (var message in _messagesBinaryToSendQueue.GetConsumingEnumerable(_cancellationAllJobs.Token))
                {
                    try
                    {
                        await SendInternalSynchronized(message).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        _logger?.Error(e, FormatLogMessage($"Failed to send binary message: '{message}'. Error: {e.Message}"));
                        _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.SendBinary));
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

                _logger?.Error(e, FormatLogMessage($"Sending binary thread failed, error: {e.Message}."));
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.BinaryQueue));
            }
        }

        private void StartBackgroundThreadForSendingText()
        {
#pragma warning disable 4014
            Task.Factory.StartNew(_ => SendTextFromQueue(), TaskCreationOptions.LongRunning, _cancellationAllJobs.Token);
#pragma warning restore 4014
        }

        private void StartBackgroundThreadForSendingBinary()
        {
#pragma warning disable 4014
            Task.Factory.StartNew(_ => SendBinaryFromQueue(), TaskCreationOptions.LongRunning, _cancellationAllJobs.Token);
#pragma warning restore 4014
        }

        private async Task SendInternalSynchronized(string message)
        {
            using (await _locker.LockAsync())
            {
                await SendInternal(message).ConfigureAwait(false);
            }
        }

        private async Task SendInternal(string message)
        {
            if (!IsConnected)
            {
                _logger?.Warn(FormatLogMessage($"Client is not connected to server, cannot send:  {message}"));
                return;
            }

            _logger?.Log(FormatLogMessage($"Sending:  {message}"));

            var buffer = MessageEncoding.GetBytes(message);

            await _socket
                .SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cancellationCurrentJobs.Token)
                .ConfigureAwait(false);
        }

        private async Task SendInternalSynchronized(byte[] message)
        {
            using (await _locker.LockAsync())
            {
                await SendInternal(message).ConfigureAwait(false);
            }
        }

        private async Task SendInternal(byte[] message)
        {
            if (!IsConnected)
            {
                _logger?.Warn(FormatLogMessage($"Client is not connected to server, cannot send binary, length: {message.Length}"));
                return;
            }

            _logger?.Log(FormatLogMessage($"Sending binary, length: {message.Length}"));

            await _socket
                .SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Binary, true, _cancellationCurrentJobs.Token)
                .ConfigureAwait(false);
        }
    }
}