using TestBucket.Ntp.Core.Client;

namespace TestBucket.Ntp.Services.Upstream
{
    public class UpstreamServerTimeUpdater : BackgroundService
    {
        private readonly ILogger<UpstreamServerTimeUpdater> _logger;
        private readonly UpstreamTimeProvider _timeProvider;

        public UpstreamServerTimeUpdater(ILogger<UpstreamServerTimeUpdater> logger, UpstreamTimeProvider timeProvider)
        {
            _logger = logger;
            _timeProvider = timeProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            string upstreamServer = "time.windows.com";
            var client = new NtpClient();

            while(!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Querying upstream NTP server {Server} for time update.", upstreamServer);
                    var response = await client.QueryAsync(upstreamServer);

                    _logger.LogInformation("Received time from upstream server: {Time}, Round Trip Delay: {Delay} ms", response.CalculatedTime, response.RoundTripDelay.TotalMilliseconds);
                    _timeProvider.ReportUpstreamTime(response.CalculatedTime);

                    await Task.Delay(TimeSpan.FromHours(1));
                }
                catch (Exception)
                {
                    await Task.Delay(TimeSpan.FromMinutes(10));

                }
            }
        }
    }
}
