namespace RxWebSocket
{
    public enum SendMethod
    {
        SingleQueue, // SendMessage
        DoubleQueue, // ArraySegment + string
        BinaryOnly,  // ArraySegment
        TextOnly     // string
    }

    public class SendingOptions
    {  
        public SendMethod SendMethod { get; }

    }
}