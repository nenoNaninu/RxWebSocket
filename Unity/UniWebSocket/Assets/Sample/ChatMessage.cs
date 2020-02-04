namespace RxWebSocket.Sample
{
    public class ChatMessage
    {
        public string Name { get; }
        public string Message { get; }

        public ChatMessage(string name, string message)
        {
            Name = name;
            Message = message;
        }
    }
}