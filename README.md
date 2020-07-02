# RxWebSocket
RxWebSocket is a WebSocket client for Unity and .NET Standard2.0/2.1. Since Unity 2018 supports .NET Standard 2.0, [ClientWebSocket](https://docs.microsoft.com/ja-jp/dotnet/api/system.net.websockets.clientwebsocket?view=netstandard-2.0) can be used. Therefore, WebSocket protocol can be used without an extra library. However, [ClientWebSocket](https://docs.microsoft.com/ja-jp/dotnet/api/system.net.websockets.clientwebsocket?view=netstandard-2.0) is very cumbersome.
RxWebSocket is a wrapper of ClientWebSocket for easy handling.

# Tested platform(Unity)
| &nbsp; |  UWP  |  iOS  | Android | Windows10 Standalone | Mac OSX Standalone | 
| ---- | ---- | ---- | ---- | ---- |-----|
| IL2CPP | ⭕️ | ⭕️ | &nbsp; | &nbsp; ||
| Mono <br>(.NET Standard2.0 & .NET 4.x) | &nbsp; | &nbsp; | ⭕️ | ⭕️ | ⭕️|
| .NET Backend | ❌ | &nbsp; | &nbsp; | &nbsp; | &nbsp;|

# Download
## Unity
[Releases page](https://github.com/nenoNaninu/RxWebSocket/releases)

## .NET Standard 2.0/2.1
```
dotnet add package RxWebSocket
```

# Requirements for Unity
- [UniRx](https://github.com/neuecc/UniRx/releases)

# Usage
The main class is `WebSocketClient`.
Various settings can be made in the constructor.
- Logger
  - If you are using unity, you can use the `UnityConsoleLogger` class. It is a simple wrapper such as Debug.log ().
  - if you use `Microsoft.Extensions.Logging.ILogger<T>` in ASP.NET Core or Console environment, it is easy to integrate using  `AsWebSocketLogger()` which is extension method.
- [Memory settings for receiving](#memory-settings-for-receiving). 
  - You can set how to use the memory when receiving messages.
  - By default, a 64KB buffer is allocated. The buffer is automatically expanded as needed.
  - If you know that you will receive large messages, a larger initial memory allocation will reduce wasted allocation.
- [Message sending method setting](#sending-options).
  - Internally, System.Threading.Channels is run in the background to send messages. 
  - You can choose various sending methods by using [these classes](#sending-options).
- [WebSocket Options(KeepAliveInterval, Proxy,...)](#websocket-options)
  - If you want to make detailed settings for WebSocket, use the factory method. This is because the `class ClientWebSocketOptions`  constructor is interlnal. You can set [like this](#websocket-options).


## Client Code
```csharp
#if Unity
var webSocketClient = new WebSocketClient(new Uri("wss://echo.websocket.org/"), logger: new UnityConsoleLogger());
#else 
Microsoft.Extensions.Logging.ILogger<T> logger;
var webSocketClient = new WebSocketClient(new Uri("wss://echo.websocket.org/"), logger: logger.AsWebSocketLogger());
#endif

//IObservable<byte[]>
webSocketClient.BinaryMessageReceived
    .Subscribe(x => DoSomething(x));

// IObservable<string>
webSocketClient.TextMessageReceived
    .Subscribe(x => DoSomething(x));

// IObservable<byte[]>
webSocketClient.RawTextMessageReceived
    .Subscribe(x => DoSomething(x));

/// Invoke when a close message is received,
/// before disconnecting the connection in normal system.
webSocketClient.CloseMessageReceived
    .Subscribe(x => DoSomething(x));

webSocketClient.OnDispose
    .Subscribe(_ => DoSomething());

//Issued when an exception occurs in processing of the background thread(receiving and sending). 
webSocketClient.ExceptionHappenedInBackground
    .Subscribe(x => DoSomething(x));

try
{
    //connect and start listening in background thread.
    //await until websocket can connect.
    await webSocketClient.ConnectAsync();

    
    //Send() method return bool.
    //If you set the channel created by Channel.CreateBounded () when setting the transmission method, 
    //false may be returned if it is not possible to write to the queue.
    //Send() function end as soon as writing is completed in the queue. 
    //Therefore, it does not matter whether the sending is finished.
    byte[] array = MakeSomeArray();
    bool check1 = webSocketClient.Send(array);
    bool check2 = webSocketClient.Send("string or byte[]");

    //The SendInstant function ignores the queue used inside the Send function and sends it immediately.
    //await for transmission to complete.
    await webSocketClient.SendInstant("string or byte[]");

    //You can decide whether to dispose at the same time as Close with the last bool parameter.
    await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "description", true);
}
catch
{
}

```
## Server Code ([ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet))
```csharp
HttpContext context;
Microsoft.Extensions.Logging.ILogger<T> logger;

if (!context.WebSockets.IsWebSocketRequest) return;

WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();

//You can set the connected socket in the constructor.
using var webSocketClient = new WebSocketClient(socket, logger: logger.AsWebSocketLogger());

// subscribe setting...

await webSocketClient.ConnectAsync();

//If you do not wait, the connection will be disconnected.
await webSocketClient.WaitUntilCloseAsync();
```
[Here](Sample/Server/WebSocketChat/WebSocketChat/WebSocketChat/WebSocketChatMiddleware.cs#L29-L47) is sample.

# Memory settings for receiving.
You can set how to use the memory when receiving messages.
By default, a 64KB buffer is allocated. The buffer is automatically expanded as needed.
If you know that you will receive large messages, a larger initial memory allocation will reduce wasted allocation.
You can use `ReceiverMemoryConfig`.
```csharp
// 1024KB
var config = new ReceiverMemoryConfig(1024 * 1024);
var client = new WebSocketClient(uri, receiverConfig: config);
```

# Sending options
Internally, System.Threading.Channels is run in the background to send messages. 
You can choose various sending methods by using the following class.

- `SingleQueueSender`
  - By using a single queue, the sending order of both Binary type and Text Type is guaranteed.
  - It is default.
- `DoubleQueueSender`
  - By using two queues, the sending order of Binary type and Text Type is guaranteed separately.
- `BinaryOnlySender`
  - Use one queue to send only binary type messages. It is less allocations than SingleQueueSender.
- `TextOnlySender`
  - Use one queue to send only text type messages. It is less allocations than SingleQueueSender.

The above class can inject a Channel in its constructor. By default Channel.CreateUnbounded is used, but if you want to limit the capacity you can use Channel.CreateBounded. At that time, it is recommended to set `SingleReader = true, SingleWriter = false` in the options of `Channel` class.

```csharp
var client = new WebSocketClient(new Uri(uri), new DoubleQueueSender());

var channel = Channel.CreateBounded<SentMessage>(new BoundedChannelOptions(5) { SingleReader = true, SingleWriter = false });
var client = new WebSocketClient(new Uri(uri), new SingleQueueSender(channel));
```

# WebSocket options
If you want to make detailed settings for WebSocket, use the factory method.
```csharp
var factory = new Func<ClientWebSocket>(() => new ClientWebSocket
{
    Options =
    {
        KeepAliveInterval = TimeSpan.FromSeconds(5),
        Proxy = ...
        ClientCertificates = ...
    }
});

var webSocketClient = new WebSocketClient(url, clientFactory: factory);
```

# Notice for Unity
WebSocketClient issues all events from thread pool. Therefore, you cannot operate the components on Unity in Subscribe directly. So please handle from the main thread using an operator such as 'ObserveOnMainThread' as follows.
```csharp
//error will occur.
webSocketClient.TextMessageReceived
    .Subscribe(x => unityObj.text = x);
    
//The following is correct.
webSocketClient.TextMessageReceived
    .ObserveOnMainThread()
    .Subscribe(x => unityObj.text = x);
```
# Sample
I prepared a simple chat app as a sample. When the server starts, connect to ```ws://ip:port/ws```.
## Unity 2018
open [this scene](https://github.com/nenoNaninu/RxWebSocket/tree/master/Unity/UniWebSocket/Assets/Scenes) and run.


## Server(C#/ ASP.NET Core3.1)
Requires [.NET Core3.1](https://dotnet.microsoft.com/download/dotnet-core/3.0).  First, set your ip [here](https://github.com/nenoNaninu/UniWebSocket/blob/master/Sample/Server/WebSocketChat/WebSocketChat/Program.cs#L23).
Then type the following command
```
$ cd Sample/Server/WebSocketChat/WebSocketChat/
$ dotnet run
```

## Web(bonus)
open [```Sample/Web/WebSocketChat.html```](Sample/Web/WebSocketChat.html)