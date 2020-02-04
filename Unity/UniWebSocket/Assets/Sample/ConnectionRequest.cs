namespace RxWebSocket.Sample
{
    public class ConnectionRequest
    {
        public string Uri { get; }
        public string Name { get; }

        public ConnectionRequest(string uri, string name)
        {
            Uri = uri;
            Name = name;
        }
    }
}