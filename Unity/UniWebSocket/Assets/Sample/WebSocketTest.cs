using System;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RxWebSocket.Logging;
using UniRx;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace RxWebSocket.Sample
{
    public class WebSocketTest : MonoBehaviour
    {
        private WebSocketClient _client;
        [SerializeField] private Transform _transform;

        // Start is called before the first frame update
        async void Start()
        {
            // var webSocketClient = new WebSocketClient(new Uri(""), () => new ClientWebSocket
            // {
            //     Options =
            //     {
            //         KeepAliveInterval = TimeSpan.FromSeconds(5),
            //     }
            // });

//            var url = new Uri("ws://echo.websocket.org");
            var url = new Uri("wss://echo.websocket.org/");

            _client = new WebSocketClient(url, new UnityConsoleLogger());

            _client.Send("いやっほーーーー");

            _client.MessageReceived
                .ObserveOn(Scheduler.MainThread)
                .Subscribe(msg =>
                {
                    Debug.Log(msg.MessageType);
                    Debug.Log($"Message received: {msg}");
                    _transform.position += Vector3.up * 10;
                })
                .AddTo(this);

            await _client.ConnectAndStartListening();
            _client.Send("はじめまーす");
        }

        private async void OnDestroy()
        {
            if (_client != null)
            {
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "stooooooooooooop!");
            }

            _client?.Dispose();
        }
    }
}