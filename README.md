# UniWebSocket
UniWebSocket is a WebSocket client for Unity. Since Unity 2018 supports .NET Standard 2.0, [ClientWebSocket](https://docs.microsoft.com/ja-jp/dotnet/api/system.net.websockets.clientwebsocket?view=netstandard-2.0) can be used. Therefore, WebSocket protocol can be used without an extra library. However, [ClientWebSocket](https://docs.microsoft.com/ja-jp/dotnet/api/system.net.websockets.clientwebsocket?view=netstandard-2.0) is very cumbersome.
UniWebSocket is a wrapper of ClientWebSocket for easy handling. UniWebSocket was created with reference to the [websocket-client](https://github.com/Marfusios/websocket-client) (Released under [the MIT License](https://github.com/Marfusios/websocket-client/blob/master/LICENSE)).


# Requirements
- UniRx
[This package](https://github.com/nenoNaninu/UniWebSocket/releases) requires [UniRx](https://github.com/neuecc/UniRx/releases), so download and import it into your project.
# How to use

```csharp
var webSocketClient = new WebSocketClient(new Uri("wss://~~~"));

//binary receive
webSocketClient.MessageReceived
    .Where(x => x.MessageType == WebSocketMessageType.Binary)
    .Select(x => x.Binary)
    .Subscribe(x => DoSometine(x));
//↑↓same
webSocketClient.BinaryMessageReceived
    .Subscribe(x => DoSomething(x));
    
//text receive
webSocketClient.TextMessageReceived
    .Subscribe(x => DoSomething(x));

webSocketClient.DisconnectionHappened
    .Subscribe(x => DoSomething(x));

webSocketClient.ErrorHappened
    .Subscribe(x => DoSomething(x));

//start connect and start listening in background thread
//Task.CompletedTask will return when it starts to be received.
//It will not be blocked while receiving.
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
```
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
The maximum size of messages to be received is set to 512KB by default. If you need more, set it with the following constructor.
```csharp=
public WebSocketClient(Uri url, int maxReceivedMessageSize, ILogger logger = null, Func<ClientWebSocket> clientFactory = null)
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
open [this scene](https://github.com/nenoNaninu/UniWebSocket/tree/master/Unity/UniWebSocket/Assets/Scenes) and run.


## Server(C#/ ASP.NET Core3.0)
Requires [.NET Core3.0](https://dotnet.microsoft.com/download/dotnet-core/3.0).  First, set your ip [here](https://github.com/nenoNaninu/UniWebSocket/blob/master/Sample/Server/WebSocketChat/WebSocketChat/Program.cs#L23).
Then type the following command
```
$ cd Sample/Server/WebSocketChat/WebSocketChat/
$ dotnet run
```

## Web(bonus)
open ```UniWebSocket/Sample/Web/WebSocketChat.html```
