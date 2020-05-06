namespace RxWebSocket
{
    public class ReceiverMemoryConfig
    {
        public static ReceiverMemoryConfig Default { get; } = new ReceiverMemoryConfig(64 * 1024, 4 * 1024);

        /// <summary>
        /// initial memory pool size for receive. default is 64 * 1024 byte(64KB)
        /// if lack of memory, memory pool is increase so allocation occur.
        /// </summary>
        public int InitialMemorySize { get; }
        
        /// <summary>
        /// if use ClientWebSocketOptions.SetBuffer(int receiveBufferSize, int sendBufferSize) in clientFactory, set this argument.
        /// default is 4 * 1024.
        /// </summary>
        public int MarginSize { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="initialMemorySize"></param>
        /// <param name="marginSize">if use ClientWebSocketOptions.SetBuffer(int receiveBufferSize, int sendBufferSize) in clientFactory, set this argument.</param>
        public ReceiverMemoryConfig(int initialMemorySize, int marginSize = 4 * 1024)
        {
            InitialMemorySize = initialMemorySize;
            MarginSize = marginSize;
        }
    }
}