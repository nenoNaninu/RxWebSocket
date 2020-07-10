using System;
using System.Linq;
using System.Net.WebSockets;
using RxWebSocket.Logging;
using UniRx;
using UnityEngine;
using Utf8Json;

namespace RxWebSocket.Sample
{
    public class WebSocketTest : MonoBehaviour
    {
        private WebSocketClient _client;
        [SerializeField] private Transform _transform;

        // Start is called before the first frame update
        //async 
            void Start()
            {
                //Observable.Timer(TimeSpan.FromSeconds(5))
                //    .Subscribe(x =>
                //    {
                //        try
                //        {
                //            var subject = new Subject<Unit>();
                //            subject.Subscribe(
                //                _ =>
                //                {
                //                    Debug.Log("叩かれた : on next");
                //                    throw new Exception("OnNextの中でエラー");
                //                },
                //                e => Debug.Log("on error!!!!!" + e.Message),
                //                () =>
                //                {
                //                    Debug.Log("叩かれた : OnCompleted");
                //                    throw new Exception("OnCompletedの中でエラー");
                //                });
                //            subject.OnNext(Unit.Default);
                //            subject.OnCompleted();
                //        }
                //        catch (Exception e)
                //        {
                //            Debug.LogWarning(e.Message);
                //        }
                //    });

                //return;

                //// var webSocketClient = new WebSocketClient(new Uri(""), () => new ClientWebSocket
                //// {
                ////     Options =
                ////     {
                ////         KeepAliveInterval = TimeSpan.FromSeconds(5),
                ////     }
                //// });
                ////var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
                ////{
                ////    Options =
                ////    {
                ////        KeepAliveInterval = TimeSpan.FromSeconds(5),
                ////        //Proxy = ...
                ////        //ClientCertificates = ...
                ////    }
                ////});
                ////            var url = new Uri("ws://echo.websocket.org");
                //var url = new Uri("wss://echo.websocket.org/");

                //_client = new WebSocketClient(url, receiverConfig: new ReceiverMemoryConfig(8 * 1024), logger: new UnityConsoleLogger());

                ////_client.Send("いやっほーーーー");

                ////_client.TextMessageReceived
                ////    .ObserveOn(Scheduler.MainThread)
                ////    .Subscribe(msg =>
                ////    {
                ////        Debug.Log($"Message received: {msg}");
                ////        _transform.position += Vector3.up * 10;
                ////    })
                ////    .AddTo(this);

                //await _client.ConnectAsync();
                ////_client.Send("はじめまーす");


                //var array = Enumerable.Range(0, 8 * 1024).ToArray();

                //_client.BinaryMessageReceived
                //    .Subscribe(x =>
                //    {
                //        var deserialize = JsonSerializer.Deserialize<int[]>(x);
                //        for (int i = 0; i < deserialize.Length; i++)
                //        {
                //            if (array[i] != deserialize[i])
                //            {
                //                Debug.LogError($"does not equal!! {array[i]}, {deserialize[i]}");
                //            }
                //        }
                //    });

                //var json_array = JsonSerializer.Serialize(array);
                //Debug.Log(json_array.Length);
                //_client.Send(json_array);
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