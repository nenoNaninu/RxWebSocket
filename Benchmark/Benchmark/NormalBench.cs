using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RxWebSocket;

namespace Benchmark
{
    [MemoryDiagnoser]
    public class NormalBench
    {
        [Benchmark]
        public async Task Bench()
        {
            var client = new WebSocketClient(new Uri("wss://echo.websocket.org/"));

            await client.ConnectAndStartListening();
            for (int i = 0; i < 10000; i++)
            {
                client.Send(BitConverter.GetBytes(i));
            }

            var task = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(100);
                    if (client.QueueCount == 0)
                    {
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal~");
                        break;
                    }
                }
            });
 

            await Task.WhenAll(client.Wait, task);
        }
    }
}