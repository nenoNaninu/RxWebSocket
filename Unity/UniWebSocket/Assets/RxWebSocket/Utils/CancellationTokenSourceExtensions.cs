using System.Runtime.CompilerServices;
using System.Threading;

namespace RxWebSocket.Utils
{
    internal static class CancellationTokenSourceExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CancelWithoutException(this CancellationTokenSource cancellationTokenSource)
        {
            if (cancellationTokenSource.IsCancellationRequested)
            {
                return;
            }

            try
            {
                cancellationTokenSource.Cancel();
            }
            catch
            {
                // ignored
            }
        }
    }
}