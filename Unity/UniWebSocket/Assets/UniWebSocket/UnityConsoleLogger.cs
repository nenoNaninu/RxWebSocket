using System;
using System.Threading;
using UniRx;
using UnityEngine;

namespace UniWebSocket
{
    public class UnityConsoleLogger : ILogger
    {
        private SynchronizationContext _context;

        public UnityConsoleLogger()
        {
            _context = SynchronizationContext.Current;
        }

        public void Log(string message)
        {
            _context.Post(_ => Debug.Log(message), null);
        }

        public void Error(string message)
        {
            _context.Post(_ => Debug.LogError(message), null);
        }

        public void Error(Exception e, string message)
        {
            _context.Post(_ =>
            {
                Debug.LogError(message);
                Debug.LogException(e);
            }, null);

            // Observable.Start(() => (message, e))
            //     .ObserveOnMainThread()
            //     .Subscribe(x =>
            //     {
            //
            //     });
        }

        public void Trace(string message)
        {
            _context.Post(_ => { Debug.Log(message); }, null);
        }
    }
}