using TestBucket.Ntp.Core.Server;

namespace TestBucket.Ntp.Core.Testing
{
    /// <summary>
    /// Provides the time that is returned to NTP clients
    /// </summary>
    public class TestTimeProvider : ITimeProvider
    {
        private readonly TimeProvider _systemTimeProvider;
        private readonly IClientRuleRepository _ruleRepository;

        public TestTimeProvider(TimeProvider systemTimeProvider, IClientRuleRepository ruleRepository)
        {
            _systemTimeProvider = systemTimeProvider;
            _ruleRepository = ruleRepository;
        }

        public async Task<NtpResponseDirective> GetTimeAsync(NtpRequestContext context, CancellationToken cancellationToken)
        {
            var rule = await _ruleRepository.GetClientRuleAsync(context.ClientAddress);
            if(rule is not null)
            {
                if(rule.ResponseDelay > TimeSpan.Zero)
                {
                    // Wait half the time here, and half later to simulate the delay being in the network and not just in the server processing
                    TimeSpan halfResponseDelay = TimeSpan.FromSeconds(rule.ResponseDelay.TotalSeconds / 2);
                    await Task.Delay(halfResponseDelay, cancellationToken);
                }
            }

            // Get the current system time
            DateTimeOffset responseTime = _systemTimeProvider.GetUtcNow();

            // Add offset
            if (rule is not null)
            {
                if (rule.ResponseDelay > TimeSpan.Zero)
                {
                    // Wait half the time here, and half earlier to simulate the delay being in the network and not just in the server processing
                    TimeSpan halfResponseDelay = TimeSpan.FromSeconds(rule.ResponseDelay.TotalSeconds / 2);
                    await Task.Delay(halfResponseDelay, cancellationToken);
                }
                responseTime = responseTime.Add(rule.TimeOffset);
            }

            NtpResponseDirective response = new NtpResponseDirective
            {
                Timestamp = responseTime
            };
            if(rule is not null)
            {
                response.ResponseLeapIndicator = rule.ResponseLeapIndicator;
                response.ResponseResourceIdentifier = rule.ResponseResourceIdentifier;
                response.ResponseStratum = rule.ResponseStratum;
                response.ResponseMode = rule.ResponseMode;
                response.ResponsePrecision = rule.ResponsePrecision;
                response.ResponseRootDelay = rule.ResponseRootDelay;
                response.ResponseRootDispersion = rule.ResponseRootDispersion;
                response.ResponsePollInterval = rule.ResponsePollInterval;
                response.ResponseTransmitTimestamp = rule.ResponseTransmitTimestamp;
                if (rule.ResponseVersionNumber.HasValue)
                {
                    response.ResponseVersionNumber = rule.ResponseVersionNumber.Value;
                }
            }

            return response;
        }
    }
}
