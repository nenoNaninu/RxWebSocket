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
    public class WebSocketClient : WebSocketClientBase<SendMessage>
    {
        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, Func<ClientWebSocket> clientFactory = null)
            : base(url, clientFactory)
        {
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, ILogger logger, Func<ClientWebSocket> clientFactory = null)
            : base(url, logger, clientFactory)
        {
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="initialMemorySize">
        /// initial memory pool size for receive. default is 64 * 1024 byte(64KB)
        /// if lack of memory, memory pool is increase so allocation occur. </param>
        /// <param name="logger"></param>
        /// <param name="clientFactory">Optional factory for native ClientWebSocket, use it whenever you need some custom features (proxy, settings, etc)</param>
        public WebSocketClient(Uri url, int initialMemorySize, ILogger logger = null, Func<ClientWebSocket> clientFactory = null)
            : base(url, initialMemorySize, logger, clientFactory)
        {
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
        public WebSocketClient(Uri url, int initialMemorySize, int receiveBufferSize, ILogger logger = null, Func<ClientWebSocket> clientFactory = null)
            : base(url, initialMemorySize, receiveBufferSize, logger, clientFactory)
        {
        }

        /// <param name="url">Target websocket url (wss://)</param>
        /// <param name="initialMemorySize">
        /// initial memory pool size for receive. default is 64 * 1024 byte(64KB)
        /// if lack of memory, memory pool is increase so allocation occur. </param>
        /// <param name="logger"></param>
        /// <param name="connectionFactory">An optional factory for creating and connecting native Websockets. The method should return connected websocket.</param>
        public WebSocketClient(Uri url, int initialMemorySize, ILogger logger, Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
            : base(url, initialMemorySize, logger, connectionFactory)
        {
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
        public WebSocketClient(Uri url, int initialMemorySize, int receiveBufferSize, ILogger logger, Func<Uri, CancellationToken, Task<WebSocket>> connectionFactory)
            : base(url, initialMemorySize, receiveBufferSize, logger, connectionFactory)
        {
        }

        /// <summary>
        /// For server(ASP.NET Core)
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="connectedSocket">Already connected socket.</param>
        public WebSocketClient(WebSocket connectedSocket, ILogger logger = null)
            : base(connectedSocket, logger)
        {
        }

        /// <summary>
        /// For server(ASP.NET Core)
        /// </summary>
        /// <param name="initialMemorySize"></param>
        /// <param name="logger"></param>
        /// <param name="connectedSocket">Already connected socket.</param>
        public WebSocketClient(WebSocket connectedSocket, int initialMemorySize, ILogger logger = null)
            : base(connectedSocket, initialMemorySize, logger)
        {
        }

        /// <summary>
        /// For server(ASP.NET Core)
        /// </summary>
        /// <param name="initialMemorySize"></param>
        /// <param name="receiveBufferSize"></param>
        /// <param name="logger"></param>
        /// <param name="connectedSocket">Already connected socket.</param>
        public WebSocketClient(WebSocket connectedSocket, int initialMemorySize, int receiveBufferSize, ILogger logger = null)
            : base(connectedSocket, initialMemorySize, receiveBufferSize, logger)
        {
        }

        public override void Send(string message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                _sendMessageQueue.Add(new SendMessage(new ArraySegment<byte>(MessageEncoding.GetBytes(message)), WebSocketMessageType.Binary));
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
        public override void Send(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                _sendMessageQueue.Add(new SendMessage(new ArraySegment<byte>(message), WebSocketMessageType.Binary));
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
        public override void Send(ArraySegment<byte> message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                _sendMessageQueue.Add(new SendMessage(message, WebSocketMessageType.Binary));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is 0 Count. Please correct it.");
            }
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        /// <param name="messageType"></param>
        public override void Send(byte[] message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                _sendMessageQueue.Add(new SendMessage(new ArraySegment<byte>(message), messageType));
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
        public override void Send(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                _sendMessageQueue.Add(new SendMessage(message, messageType));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is 0 Count. Please correct it.");
            }
        }

        /// <summary>
        /// Send text message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public override Task SendInstant(string message)
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
        public override Task SendInstant(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(new SendMessage(new ArraySegment<byte>(message), WebSocketMessageType.Binary));
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        public override Task SendInstant(byte[] message, WebSocketMessageType messageType)
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
        public override Task SendInstant(ArraySegment<byte> message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(new SendMessage(message, WebSocketMessageType.Binary));
            }

            throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is 0 Count. Please correct it.");
        }

        public override Task SendInstant(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(new SendMessage(message, messageType));
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the SendInstant function is null or 0 Length. Please correct it.");
        }
        protected override async Task SendInternal(SendMessage message)
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