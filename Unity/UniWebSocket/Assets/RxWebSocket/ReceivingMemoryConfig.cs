namespace RxWebSocket
{
    public class ReceivingMemoryConfig
    {
        public static ReceivingMemoryConfig Default { get; } = new ReceivingMemoryConfig(64 * 1024, 4 * 1024);

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

        public ReceivingMemoryConfig(int initialMemorySize, int marginSize)
        {
            InitialMemorySize = initialMemorySize;
            MarginSize = marginSize;
        }

        public ReceivingMemoryConfig(int initialMemorySize)
        {
            InitialMemorySize = initialMemorySize;
            MarginSize = 4 * 1024;
        }
    }
}