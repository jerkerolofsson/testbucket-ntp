using System.Net;
using TestBucket.Ntp.Core.Testing;
using TestBucket.Ntp.Core.UnitTests.Helpers;
using Xunit;

namespace TestBucket.Ntp.Core.UnitTests
{
    public class FileSystemClientRuleRepositoryTests
    {
        #region AddClientRuleAsync

        [Fact]
        public async Task AddClientRuleAsync_NullRule_ThrowsArgumentNullException()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);

            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.AddClientRuleAsync(null!));
        }

        [Theory]
        [InlineData("")]
        [InlineData("  ")]
        public async Task AddClientRuleAsync_EmptyIpAddress_ThrowsArgumentException(string ipAddress)
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var rule = new ClientRule { ClientIpAddress = ipAddress };

            await Assert.ThrowsAsync<ArgumentException>(() => repo.AddClientRuleAsync(rule));
        }

        [Fact]
        public async Task AddClientRuleAsync_InvalidIpAddress_ThrowsArgumentException()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var rule = new ClientRule { ClientIpAddress = "not-an-ip" };

            await Assert.ThrowsAsync<ArgumentException>(() => repo.AddClientRuleAsync(rule));
        }

        [Fact]
        public async Task AddClientRuleAsync_ValidIpv4_AddsRule()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var rule = new ClientRule { ClientIpAddress = "192.168.1.1" };

            await repo.AddClientRuleAsync(rule);

            var result = await repo.GetClientRuleAsync(IPAddress.Parse("192.168.1.1"));
            Assert.NotNull(result);
            Assert.Equal("192.168.1.1", result.ClientIpAddress);
        }

        [Fact]
        public async Task AddClientRuleAsync_ValidIpv6_AddsRule()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var rule = new ClientRule { ClientIpAddress = "::1" };

            await repo.AddClientRuleAsync(rule);

            var result = await repo.GetClientRuleAsync(IPAddress.Parse("::1"));
            Assert.NotNull(result);
            Assert.Equal("::1", result.ClientIpAddress);
        }

        [Fact]
        public async Task AddClientRuleAsync_AddingSameIpTwice_OverwritesExistingRule()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var original = new ClientRule { ClientIpAddress = "10.0.0.1", TimeOffset = TimeSpan.FromSeconds(1) };
            var updated = new ClientRule { ClientIpAddress = "10.0.0.1", TimeOffset = TimeSpan.FromSeconds(5) };

            await repo.AddClientRuleAsync(original);
            await repo.AddClientRuleAsync(updated);

            var result = await repo.GetClientRuleAsync(IPAddress.Parse("10.0.0.1"));
            Assert.NotNull(result);
            Assert.Equal(TimeSpan.FromSeconds(5), result.TimeOffset);
        }

        #endregion

        #region GetClientRuleAsync

        [Fact]
        public async Task GetClientRuleAsync_NullIpAddress_ThrowsArgumentNullException()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);

            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.GetClientRuleAsync(null!));
        }

        [Fact]
        public async Task GetClientRuleAsync_RuleDoesNotExist_ReturnsNull()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);

            var result = await repo.GetClientRuleAsync(IPAddress.Parse("1.2.3.4"));

            Assert.Null(result);
        }

        [Fact]
        public async Task GetClientRuleAsync_RuleExists_ReturnsCorrectRule()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var rule = new ClientRule
            {
                ClientIpAddress = "172.16.0.1",
                TimeOffset = TimeSpan.FromMinutes(2),
                ResponseDelay = TimeSpan.FromMilliseconds(100)
            };

            await repo.AddClientRuleAsync(rule);
            var result = await repo.GetClientRuleAsync(IPAddress.Parse("172.16.0.1"));

            Assert.NotNull(result);
            Assert.Equal(TimeSpan.FromMinutes(2), result.TimeOffset);
            Assert.Equal(TimeSpan.FromMilliseconds(100), result.ResponseDelay);
        }

        #endregion

        #region RemoveClientRuleAsync

        [Fact]
        public async Task RemoveClientRuleAsync_NullRule_ThrowsArgumentNullException()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);

            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.RemoveClientRuleAsync(null!));
        }

        [Fact]
        public async Task RemoveClientRuleAsync_EmptyIpAddress_ThrowsArgumentException()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var rule = new ClientRule { ClientIpAddress = "" };

            await Assert.ThrowsAsync<ArgumentException>(() => repo.RemoveClientRuleAsync(rule));
        }

        [Fact]
        public async Task RemoveClientRuleAsync_ExistingRule_RemovesRule()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var rule = new ClientRule { ClientIpAddress = "10.10.10.10" };

            await repo.AddClientRuleAsync(rule);
            await repo.RemoveClientRuleAsync(rule);

            var result = await repo.GetClientRuleAsync(IPAddress.Parse("10.10.10.10"));
            Assert.Null(result);
        }

        [Fact]
        public async Task RemoveClientRuleAsync_ExistingRule_DeletesFileFromDisk()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var rule = new ClientRule { ClientIpAddress = "10.10.10.10" };

            await repo.AddClientRuleAsync(rule);
            var files = Directory.GetFiles(folder.Path, "*.json");
            Assert.Single(files);

            await repo.RemoveClientRuleAsync(rule);
            files = Directory.GetFiles(folder.Path, "*.json");
            Assert.Empty(files);
        }

        [Fact]
        public async Task RemoveClientRuleAsync_NonExistentRule_DoesNotThrow()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var rule = new ClientRule { ClientIpAddress = "9.9.9.9" };

            var ex = await Record.ExceptionAsync(() => repo.RemoveClientRuleAsync(rule));
            Assert.Null(ex);
        }

        #endregion

        #region BrowseAsync

        [Fact]
        public async Task BrowseAsync_EmptyRepository_ReturnsEmptyList()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);

            var result = await repo.BrowseAsync(0, 10);

            Assert.Empty(result);
        }

        [Fact]
        public async Task BrowseAsync_MultipleRules_ReturnsOrderedByIpAddress()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "192.168.1.3" });
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "192.168.1.1" });
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "192.168.1.2" });

            var result = await repo.BrowseAsync(0, 10);

            Assert.Equal(3, result.Count);
            Assert.Equal("192.168.1.1", result[0].ClientIpAddress);
            Assert.Equal("192.168.1.2", result[1].ClientIpAddress);
            Assert.Equal("192.168.1.3", result[2].ClientIpAddress);
        }

        [Fact]
        public async Task BrowseAsync_WithOffset_SkipsRules()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "10.0.0.1" });
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "10.0.0.2" });
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "10.0.0.3" });

            var result = await repo.BrowseAsync(1, 10);

            Assert.Equal(2, result.Count);
            Assert.Equal("10.0.0.2", result[0].ClientIpAddress);
        }

        [Fact]
        public async Task BrowseAsync_WithCount_LimitsResults()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "10.0.0.1" });
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "10.0.0.2" });
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "10.0.0.3" });

            var result = await repo.BrowseAsync(0, 2);

            Assert.Equal(2, result.Count);
        }

        #endregion

        #region Persistence

        [Fact]
        public async Task AddClientRuleAsync_PersistsRuleToDisk()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            var rule = new ClientRule { ClientIpAddress = "192.168.0.1" };

            await repo.AddClientRuleAsync(rule);

            var files = Directory.GetFiles(folder.Path, "*.json");
            Assert.Single(files);
        }

        [Fact]
        public async Task Constructor_LoadsExistingRulesFromDisk()
        {
            using var folder = new TempFolder();
            var rule = new ClientRule { ClientIpAddress = "192.168.1.100", TimeOffset = TimeSpan.FromSeconds(10) };

            var repo1 = new FileSystemClientRuleRepository(folder.Path);
            await repo1.AddClientRuleAsync(rule);

            var repo2 = new FileSystemClientRuleRepository(folder.Path);
            var result = await repo2.GetClientRuleAsync(IPAddress.Parse("192.168.1.100"));

            Assert.NotNull(result);
            Assert.Equal(TimeSpan.FromSeconds(10), result.TimeOffset);
        }

        [Fact]
        public async Task AddClientRuleAsync_Ipv4_CreatesFileWithSanitizedName()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "1.2.3.4" });

            var files = Directory.GetFiles(folder.Path, "1_2_3_4.json");
            Assert.Single(files);
        }

        [Fact]
        public async Task AddClientRuleAsync_Ipv6_CreatesFileWithSanitizedName()
        {
            using var folder = new TempFolder();
            var repo = new FileSystemClientRuleRepository(folder.Path);
            await repo.AddClientRuleAsync(new ClientRule { ClientIpAddress = "::1" });

            var files = Directory.GetFiles(folder.Path, "__1.json");
            Assert.Single(files);
        }

        #endregion
    }
}
