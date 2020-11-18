using LyricParser.Services.Interfaces;
using System;
using System.Threading;
using System.Windows.Threading;

namespace LyricParser.Services
{
    public class PollingService : IPollingService
    {
        public TimeSpan Span { get; set; }
        public Action Callback { get; set; }
        private CancellationTokenSource cancellation;
        private DispatcherTimer timer;

        public PollingService()
        {
            cancellation = new CancellationTokenSource();
            timer = new DispatcherTimer();
            timer.Interval = Span;
            timer.Tick += (e, sender) =>
            {
                if (cancellation.IsCancellationRequested) timer.Stop();
                Callback.Invoke();
            };
        }

        public void Start()
        {
            timer.Start();
        }

        public void Stop()
        {
            Interlocked.Exchange(ref cancellation, new CancellationTokenSource()).Cancel();
        }
    }
}
