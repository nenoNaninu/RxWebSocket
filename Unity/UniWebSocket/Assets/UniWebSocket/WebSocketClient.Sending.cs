using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace UniWebSocket
{
    public partial class WebSocketClient
    {
        private readonly BlockingCollection<string> _messagesTextToSendQueue = new BlockingCollection<string>();
        private readonly BlockingCollection<byte[]> _messagesBinaryToSendQueue = new BlockingCollection<byte[]>();

        /// <summary>
        /// Send text message to the websocket channel. 
        /// It inserts the message to the queue and actual sending is done on an other thread
        /// </summary>
        /// <param name="message">Text message to be sent</param>
        public Task Send(string message)
        {
            Validations.Validations.ValidateInput(message, nameof(message));

            _messagesTextToSendQueue.Add(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It inserts the message to the queue and actual sending is done on an other thread
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        public Task Send(byte[] message)
        {
            Validations.Validations.ValidateInput(message, nameof(message));

            _messagesBinaryToSendQueue.Add(message);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Send text message to the websocket channel. 
        /// It doesn't use a sending queue, 
        /// beware of issue while sending two messages in the exact same time 
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(string message)
        {
            Validations.Validations.ValidateInput(message, nameof(message));

            return SendInternalSynchronized(message);
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a sending queue, 
        /// beware of issue while sending two messages in the exact same time 
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(byte[] message)
        {
            return SendInternalSynchronized(message);
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
                        _logger?.Error(FormatLogMessage($"Failed to send text message: '{message}'. Error: {e.Message}"));
                        _exceptionSubject.OnNext(new WebSocketErrorChunk(e, ErrorType.SendText));
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
                if (_cancellationAllJobs.IsCancellationRequested || _disposing)
                {
                    // disposing/canceling, do nothing and exit
                    return;
                }

                _logger?.Trace(FormatLogMessage($"Sending text thread failed, error: {e.Message}. Creating a new sending thread."));
                StartBackgroundThreadForSendingText();
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
                        _logger?.Error(FormatLogMessage($"Failed to send binary message: '{message}'. Error: {e.Message}"));
                        _exceptionSubject.OnNext(new WebSocketErrorChunk(e, ErrorType.SendBinary));
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
                if (_cancellationAllJobs.IsCancellationRequested || _disposing)
                {
                    // disposing/canceling, do nothing and exit
                    return;
                }

                _logger?.Trace(FormatLogMessage($"Sending binary thread failed, error: {e.Message}. Creating a new sending thread."));
                StartBackgroundThreadForSendingBinary();
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
                await SendInternal(message);
            }
        }

        private async Task SendInternal(string message)
        {
            if (!IsClientConnected())
            {
                _logger?.Log(FormatLogMessage($"Client is not connected to server, cannot send:  {message}"));
                return;
            }

            _logger?.Trace(FormatLogMessage($"Sending:  {message}"));
            var buffer = MessageEncoding.GetBytes(message);
            var messageSegment = new ArraySegment<byte>(buffer);
            await _client
                .SendAsync(messageSegment, WebSocketMessageType.Text, true, _cancellationCurrentJobs.Token)
                .ConfigureAwait(false);
        }

        private async Task SendInternalSynchronized(byte[] message)
        {
            using (await _locker.LockAsync())
            {
                await SendInternal(message);
            }
        }

        private async Task SendInternal(byte[] message)
        {
            if (!IsClientConnected())
            {
                _logger?.Log(FormatLogMessage($"Client is not connected to server, cannot send binary, length: {message.Length}"));
                return;
            }

            _logger?.Trace(FormatLogMessage($"Sending binary, length: {message.Length}"));

            await _client
                .SendAsync(new ArraySegment<byte>(message), WebSocketMessageType.Binary, true, _cancellationCurrentJobs.Token)
                .ConfigureAwait(false);
        }
    }
}