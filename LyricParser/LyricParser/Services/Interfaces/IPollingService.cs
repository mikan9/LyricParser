using System;

namespace LyricParser.Services.Interfaces
{
    public interface IPollingService
    {
        TimeSpan Span { get; set; }
        Action Callback { get; set; }
        void Start();
        void Stop();
    }
}
