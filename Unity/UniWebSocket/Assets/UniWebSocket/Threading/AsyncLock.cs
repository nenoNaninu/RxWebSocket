using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniWebSocket.Threading
{
    /// <summary>
    /// Example:
    /// <code>
    /// using(await _asyncLock.LockAsync())
    /// {
    ///     // do your synchronized work
    /// }
    /// </code>
    /// </summary>
    public class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore;
        private readonly LockReleaser _lockReleaser;

        public AsyncLock()
        {
            _semaphore = new SemaphoreSlim(1, 1);
            _lockReleaser = new LockReleaser(_semaphore);
        }

        public async Task<IDisposable> LockAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);
            return _lockReleaser;
        }

        private class LockReleaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

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