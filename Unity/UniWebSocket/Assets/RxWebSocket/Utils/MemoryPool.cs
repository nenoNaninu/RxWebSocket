using System;
using RxWebSocket.Logging;

namespace RxWebSocket.Utils
{
    public class MemoryPool
    {
        private byte[] _memoryPool;
        private int _offset = 0;

        private readonly int _margin;
        private readonly ILogger _logger;

        public int Offset
        {
            get => _offset;
            set
            {
                _offset = value;

                if (_offset + _margin <= _memoryPool.Length)
                {
                    return;
                }

                _logger?.Log($"[WEBSOCKET MEMORY POOL] resize pool : {_memoryPool.Length} bytes to {_memoryPool.Length * 2} bytes");

                var newArrayPool = new byte[_memoryPool.Length * 2];
                Buffer.BlockCopy(_memoryPool, 0, newArrayPool, 0, _offset);
                _memoryPool = newArrayPool;
            }
        }

        public MemoryPool(int initialSize, int margin, ILogger logger = null)
        {
            _memoryPool = new byte[initialSize];
            _margin = margin;
            _logger = logger;
        }

        public ArraySegment<byte> SliceFromOffset() => new ArraySegment<byte>(_memoryPool, _offset, _memoryPool.Length - _offset);

        public byte[] ToArray()
        {
            var array = new byte[_offset];
            Buffer.BlockCopy(_memoryPool, 0, array, 0, _offset);
            return array;
        }
    }
}