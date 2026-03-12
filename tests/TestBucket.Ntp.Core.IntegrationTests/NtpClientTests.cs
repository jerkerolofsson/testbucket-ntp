using TestBucket.Ntp.Core.Client;

namespace TestBucket.Ntp.Core.IntegrationTests
{
    public class NtpClientTests
    {
        private static readonly TimeSpan MaxAllowedDrift = TimeSpan.FromMinutes(10);

        [Theory]
        [InlineData("time.google.com")]
        [InlineData("pool.ntp.org")]
        [InlineData("time.cloudflare.com")]
        [InlineData("time.windows.com")]
        public async Task GetServerTimeAsync_ShouldReturnTimeWithinAcceptableDrift(string ntpServer)
        {
            // Arrange
            var client = new NtpClient();
            var systemTime = DateTimeOffset.UtcNow;

            // Act
            var ntpTime = await client.GetServerTimeAsync(ntpServer);

            // Assert
            var timeDifference = (ntpTime - systemTime).Duration();
            Assert.True(
                timeDifference <= MaxAllowedDrift,
                $"NTP server '{ntpServer}' time differs from system time by {timeDifference.TotalMinutes:F2} minutes, which exceeds the maximum allowed drift of {MaxAllowedDrift.TotalMinutes} minutes. NTP Time: {ntpTime:O}, System Time: {systemTime:O}");
        }

        [Fact]
        public async Task GetServerTimeAsync_MultipleCalls_ShouldReturnConsistentTimes()
        {
            // Arrange
            var client = new NtpClient();
            const string ntpServer = "time.google.com";

            // Act
            var time1 = await client.GetServerTimeAsync(ntpServer);
            await Task.Delay(100); // Small delay between calls
            var time2 = await client.GetServerTimeAsync(ntpServer);

            // Assert
            var timeDifference = (time2 - time1).Duration();
            Assert.True(
                timeDifference < TimeSpan.FromSeconds(5),
                $"Two consecutive calls to the same NTP server returned times that differ by {timeDifference.TotalSeconds:F2} seconds, which is unexpected.");
        }

        [Fact]
        public async Task GetServerTimeAsync_ShouldReturnUtcTime()
        {
            // Arrange
            var client = new NtpClient();
            const string ntpServer = "time.google.com";

            // Act
            var ntpTime = await client.GetServerTimeAsync(ntpServer);

            // Assert
            Assert.Equal(TimeSpan.Zero, ntpTime.Offset);
        }

        [Fact]
        public async Task GetServerTimeAsync_InvalidHostname_ShouldThrowException()
        {
            // Arrange
            var client = new NtpClient();
            const string invalidServer = "invalid.ntp.server.that.does.not.exist.example";

            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(() => client.GetServerTimeAsync(invalidServer));
        }

        [Theory]
        [InlineData("time.nist.gov")]
        [InlineData("time.apple.com")]
        public async Task GetServerTimeAsync_AdditionalPublicServers_ShouldReturnValidTime(string ntpServer)
        {
            // Arrange
            var client = new NtpClient();
            var systemTime = DateTimeOffset.UtcNow;

            // Act
            var ntpTime = await client.GetServerTimeAsync(ntpServer);

            // Assert
            Assert.NotEqual(DateTimeOffset.MinValue, ntpTime);
            Assert.NotEqual(DateTimeOffset.MaxValue, ntpTime);

            var timeDifference = (ntpTime - systemTime).Duration();
            Assert.True(
                timeDifference <= MaxAllowedDrift,
                $"NTP server '{ntpServer}' time differs from system time by {timeDifference.TotalMinutes:F2} minutes.");
        }
    }
}
