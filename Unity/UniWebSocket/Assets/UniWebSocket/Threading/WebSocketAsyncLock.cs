using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniWebSocket.Threading
{
    /// <summary>
    /// Class that wraps SemaphoreSlim and enables to use locking inside 'using' blocks easily
    /// Don't need to bother with releasing and handling SemaphoreSlim correctly
    /// Example:
    /// <code>
    /// using(await _asyncLock.LockAsync())
    /// {
    ///     // do your synchronized work
    /// }
    /// </code>
    /// </summary>
    public class WebSocketAsyncLock
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly LockReleaser _lockReleaser;

        public WebSocketAsyncLock()
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _lockReleaser = new LockReleaser(_semaphore);
        }

        public async Task<LockReleaser> LockAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            return _lockReleaser;
        }

        public class LockReleaser : IDisposable
        {
            readonly SemaphoreSlim _semaphore;

            public LockReleaser(SemaphoreSlim semaphore)
            {
                _semaphore = semaphore;
            }

            public void Dispose()
            {
                _semaphore?.Release();
            }
        }
    }
}