using TestBucket.Ntp.Core.Server;

namespace TestBucket.Ntp.Services
{
    public class NtpServerLifetimeBackgroundService : BackgroundService
    {
        private readonly NtpServer _server;

        public NtpServerLifetimeBackgroundService(NtpServer server)
        {
            _server = server;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _server.Start();

                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }

            }
            finally
            {
                await _server.StopAsync();
                _server.Dispose();
            }
        }
    }
}
