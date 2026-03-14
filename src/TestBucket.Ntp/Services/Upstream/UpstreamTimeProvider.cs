using System.Diagnostics;

namespace TestBucket.Ntp.Services.Upstream
{
    public class UpstreamTimeProvider : TimeProvider
    {
        /// <summary>
        /// Gets/sets the current time retreived from upstream providers
        /// </summary>
        private DateTimeOffset? _time { get; set; } = null;

        /// <summary>
        /// Keep track of the timestamp when the time was reported so we can add the elapsed time
        /// to the reported time.
        /// </summary>
        private long _reportTimestamp = 0;

        /// <summary>
        /// Invoked by a background service that fetches the time from an upstream server
        /// </summary>
        /// <param name="calculatedTime"></param>
        public void ReportUpstreamTime(DateTimeOffset calculatedTime)
        {
            _reportTimestamp = Stopwatch.GetTimestamp();
            _time = calculatedTime;
        }

        public override DateTimeOffset GetUtcNow()
        {
            if(_time is null)
            {
                return base.GetUtcNow();
            }

            var elapsed = Stopwatch.GetElapsedTime(_reportTimestamp);
            if(elapsed > TimeSpan.FromDays(7))
            {
                return base.GetUtcNow();
            }

            return _time.Value.Add(elapsed);
        }
    }
}
