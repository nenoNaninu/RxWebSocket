using System;
using System.Threading.Tasks;
using RxWebSocket.Logging;

namespace RxWebSocket.Threading
{
    internal static class TaskExtensions
    {
        internal static async void Forget(this Task task, ILogger logger)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger?.Error(e, e.Message);
            }
        }
    }
}