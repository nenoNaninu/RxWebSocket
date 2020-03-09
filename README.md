# RxWebSocket
RxWebSocket is a WebSocket client for Unity and .NET Standard2.0/2.1. Since Unity 2018 supports .NET Standard 2.0, [ClientWebSocket](https://docs.microsoft.com/ja-jp/dotnet/api/system.net.websockets.clientwebsocket?view=netstandard-2.0) can be used. Therefore, WebSocket protocol can be used without an extra library. However, [ClientWebSocket](https://docs.microsoft.com/ja-jp/dotnet/api/system.net.websockets.clientwebsocket?view=netstandard-2.0) is very cumbersome.
RxWebSocket is a wrapper of ClientWebSocket for easy handling. RxWebSocket was created with reference to the [websocket-client](https://github.com/Marfusios/websocket-client) (Released under [the MIT License](https://github.com/Marfusios/websocket-client/blob/master/LICENSE)).

# Tested platform(Unity)
- UWP
  -  ⭕️: IL2CPP
     -  .NET Standard 2.0 & .NET 4.x
  -  ❌: .NET Backend
  
- iOS
  - ⭕️: IL2CPP
    - .NET Standard 2.0

- Mac OSX Standalone
  -  ⭕️: Mono
     -  .NET Standard.0 & .NET 4.x
  -  ❌: IL2CPP

- Windows10 Standalone
  -  ⭕️: Mono
     -  .NET Standard 2.0 & .NET 4.x

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
We prepared two classes, `WebSocketClient` and `BinaryWebSocketClient`.
`WebSocketClient` is common, but if you only send binary type, you can use `BinaryWebSocketClient`. 
`BinaryWebSocketClient` can send only binary type, but has less allocation.

## Client
```csharp
#if Unity
var webSocketClient = new WebSocketClient(new Uri("wss://~~~"), new UnityConsoleLogger());
//or
var binaryWebSocketClient = new BinaryWebSocketClient(new Uri("wss://~~~"), new UnityConsoleLogger());
#else 
Microsoft.Extensions.Logging.ILogger<T> logger;
var webSocketClient = new WebSocketClient(new Uri("wss://~~~"), logger.AsWebSocketLogger());
#endif

webSocketClient.BinaryMessageReceived
    .Subscribe(x => DoSomething(x));
    
webSocketClient.TextMessageReceived
    .Subscribe(x => DoSomething(x));

webSocketClient.CloseMessageReceived
    .Subscribe(x => DoSomething(x));

webSocketClient.ExceptionHappened
    .Subscribe(x => DoSomething(x));

//connect and start listening in background thread.
//await until websocket can connect.
try
{
    await webSocketClient.ConnectAndStartListening();

    //send bin
    byte[] array = MakeSomeArray();
    webSocketClient.Send(array);

    //send text
    //The Send function guarantees the transmission order using queue.
    //It doesn't wait for the transmission to complete.
    webSocketClient.Send("string or byte[]");

    //The SendInstant function ignores the queue used inside the Send function and sends it immediately.
    //await for transmission to complete.
    await webSocketClient.SendInstant("string or byte[]");

    //You can decide whether to dispose at the same time as Close with the last bool parameter.
    await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "description", true);
}

```
## Server ([ASP.NET Core](https://dotnet.microsoft.com/apps/aspnet))
```csharp
HttpContext context;
Microsoft.Extensions.Logging.ILogger<T> logger;

if (!context.WebSockets.IsWebSocketRequest) return;

var socket = await context.WebSockets.AcceptWebSocketAsync();

using var webSocketClient = new WebSocketClient(socket, logger.AsWebSocketLogger());

// subscribe setting...

await webSocketClient.ConnectAndStartListening();

//If you do not wait, the connection will be disconnected.
await webSocketClient.Wait;
```
[Here](https://github.com/nenoNaninu/RxWebSocket/blob/master/Sample/Server/WebSocketChat/WebSocketChat/WebSocketChat/WebSocketChatMiddleware.cs#L29-L47) is sample.

## Options
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

var webSocketClient = new WebSocketClient(url, factory);
```
The default received memory pool is set to 64KB.
if lack of memory, memory pool is automatically increase so allocation occur.
If it is known that a large size will come, it is advantageous to set a large memory pool in the following constructor.
```csharp
public WebSocketClient(Uri url, int initialMemorySize, ILogger logger = null, Func<ClientWebSocket> clientFactory = null)
```

## Notice
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
open ```UniWebSocket/Sample/Web/WebSocketChat.html```
