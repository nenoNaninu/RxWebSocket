using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using RxWebSocket;


namespace Benchmark
{

    [MemoryDiagnoser]
    public class MemoryBench
    {
        private byte[] array;

        public MemoryBench()
        {
            array = Encoding.UTF8.GetBytes("aaaaaaaaaaaaaa");
        }
        [Benchmark]
        public void Bench1()
        {
            var blockingcollection = new BlockingCollection<SendMessage>();
            // var task = Task.Run(() =>
            // {
            //     foreach (var it in blockingcollection.GetConsumingEnumerable())
            //     {
            //         var tmp = it.Bytes[0] + it.Bytes[0];
            //     }
            // });
            // SendMessage[] messages = new SendMessage[10000];
            for (int i = 0; i < 1000; i++)
            {
                blockingcollection.Add(new SendMessage(new ArraySegment<byte>(array), WebSocketMessageType.Binary));
            }
            
            foreach (var it in blockingcollection.GetConsumingEnumerable())
            {
                if (blockingcollection.Count == 0)
                {
                    break;
                }
            }
            
            for (int i = 0; i < 1000; i++)
            {
                blockingcollection.Add(new SendMessage(new ArraySegment<byte>(array), WebSocketMessageType.Binary));
            }
            // await task;
        }
        
        [Benchmark]
        public void Bench2()
        {
            var blockingcollection = new BlockingCollection<ArraySegment<byte>>();
            // var task = Task.Run(() =>
            // {
            //     foreach (var it in blockingcollection.GetConsumingEnumerable())
            //     {
            //         var tmp = it[0] + it[0];
            //     }
            // });
            // SendMessage[] messages = new SendMessage[10000];
            for (int i = 0; i < 1000; i++)
            {
                blockingcollection.Add(new ArraySegment<byte>(array));
            }
            // await task;
            // for (int i = 0; i < 1000; i++)
            // {
            //     blockingcollection.Take();
            // }

            foreach (var it in blockingcollection.GetConsumingEnumerable())
            {
                if (blockingcollection.Count == 0)
                {
                    break;
                }
            }
            
            for (int i = 0; i < 1000; i++)
            {
                blockingcollection.Add(new ArraySegment<byte>(array));
            }
            
        }
    }
}