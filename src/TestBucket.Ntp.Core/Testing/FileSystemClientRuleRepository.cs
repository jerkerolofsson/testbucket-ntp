using System.Net;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace TestBucket.Ntp.Core.Testing
{
    public class FileSystemClientRuleRepository : IClientRuleRepository
    {
        private readonly Dictionary<string, ClientRule> _rules = new();
        private readonly object _lock = new();
        private readonly string _dataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "test-bucket",
                "ntp",
                "client-rules"
                );

        public FileSystemClientRuleRepository(ILogger<FileSystemClientRuleRepository> logger)
        {
            Directory.CreateDirectory(_dataFolder);
            logger.LogInformation("Initialized FileSystemClientRuleRepository with data folder: {DataFolder}", _dataFolder);
            LoadRulesFromFileSystem();
        }

        public FileSystemClientRuleRepository(string dataFolder) : base()
        {
            _dataFolder = dataFolder;
            Directory.CreateDirectory(_dataFolder);
            LoadRulesFromFileSystem();
        }

        public Task AddClientRuleAsync(ClientRule clientRule)
        {
            if (clientRule == null)
            {
                throw new ArgumentNullException(nameof(clientRule));
            }

            if (string.IsNullOrWhiteSpace(clientRule.ClientIpAddress))
            {
                throw new ArgumentException("ClientIpAddress cannot be null or empty.", nameof(clientRule));
            }

            if (!IPAddress.TryParse(clientRule.ClientIpAddress, out _))
            {
                throw new ArgumentException($"Invalid IP address: {clientRule.ClientIpAddress}", nameof(clientRule));
            }

            lock (_lock)
            {
                _rules[clientRule.ClientIpAddress] = clientRule;
                SaveRuleToFileSystem(clientRule);
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ClientRule>> BrowseAsync(int offset, int count)
        {
            lock (_lock)
            {
                var rules = _rules.Values
                    .OrderBy(r => r.ClientIpAddress)
                    .Skip(offset)
                    .Take(count)
                    .ToList();
                return Task.FromResult<IReadOnlyList<ClientRule>>(rules);
            }
        }

        public Task<ClientRule?> GetClientRuleAsync(IPAddress clientIpAddress)
        {
            if (clientIpAddress == null)
            {
                throw new ArgumentNullException(nameof(clientIpAddress));
            }

            lock (_lock)
            {
                _rules.TryGetValue(clientIpAddress.ToString(), out var rule);
                return Task.FromResult(rule);
            }
        }

        public string GetDataFolder() => _dataFolder;

        public Task RemoveClientRuleAsync(ClientRule clientRule)
        {
            if (clientRule == null)
            {
                throw new ArgumentNullException(nameof(clientRule));
            }

            if (string.IsNullOrWhiteSpace(clientRule.ClientIpAddress))
            {
                throw new ArgumentException("ClientIpAddress cannot be null or empty.", nameof(clientRule));
            }

            lock (_lock)
            {
                if (_rules.Remove(clientRule.ClientIpAddress))
                {
                    DeleteRuleFromFileSystem(clientRule.ClientIpAddress);
                }
            }

            return Task.CompletedTask;
        }

        private void LoadRulesFromFileSystem()
        {
            var folder = GetDataFolder();
            if (!Directory.Exists(folder))
            {
                return;
            }

            var jsonFiles = Directory.GetFiles(folder, "*.json");
            foreach (var file in jsonFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var rule = JsonSerializer.Deserialize<ClientRule>(json);
                    if (rule != null && !string.IsNullOrWhiteSpace(rule.ClientIpAddress))
                    {
                        _rules[rule.ClientIpAddress] = rule;
                    }
                }
                catch
                {
                    // Skip invalid files
                }
            }
        }

        private void SaveRuleToFileSystem(ClientRule clientRule)
        {
            var fileName = GetFileNameForIpAddress(clientRule.ClientIpAddress);
            var filePath = Path.Combine(GetDataFolder(), fileName);
            var json = JsonSerializer.Serialize(clientRule, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(filePath, json);
        }

        private void DeleteRuleFromFileSystem(string ipAddress)
        {
            var fileName = GetFileNameForIpAddress(ipAddress);
            var filePath = Path.Combine(GetDataFolder(), fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private static string GetFileNameForIpAddress(string ipAddress)
        {
            // Replace colons and periods with underscores to create valid file names
            var sanitized = ipAddress.Replace(":", "_").Replace(".", "_");
            return $"{sanitized}.json";
        }
    }
}
