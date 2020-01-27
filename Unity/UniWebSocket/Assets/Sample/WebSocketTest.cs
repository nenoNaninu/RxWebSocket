using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

namespace UniWebSocket.Sample
{
    public class WebSocketTest : MonoBehaviour
    {
        private WebSocketClient _client;
        [SerializeField] private Transform _transform;

        // Start is called before the first frame update
        async void Start()
        {
            var webSocketClient = new WebSocketClient(new Uri(""), () => new ClientWebSocket
            {
                Options =
                {
                    KeepAliveInterval = TimeSpan.FromSeconds(5),
                }
            });

//            var url = new Uri("ws://echo.websocket.org");
            var url = new Uri("ws://192.168.0.3:8080/ws");

            _client = new WebSocketClient(url, new UnityConsoleLogger());

            _client.Send("いやっほーーーー");

            _client.MessageReceived
                .ObserveOn(Scheduler.MainThread)
                .Subscribe(msg =>
                {
                    Debug.Log($"Message received: {msg}");
                    _transform.position += Vector3.up * 10;
                })
                .AddTo(this);

            Task.Run(async () =>
            {
                await Task.Delay(5000);
                _client.Send("Delay Message!!!!!!!!!!!!!!!!");
            });

            await _client.ConnectAndStartListening();
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