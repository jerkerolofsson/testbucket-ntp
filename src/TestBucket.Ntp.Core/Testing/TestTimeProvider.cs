using TestBucket.Ntp.Core.Server;

namespace TestBucket.Ntp.Core.Testing
{
    /// <summary>
    /// Provides the time that is returned to NTP clients
    /// 
    /// When fetching the time the client rule is fetched (if any, depending on the client IP) and the response is modified according to the rule (e.g. adding an offset, or adding a delay before responding)
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

       
        /// <summary>
        /// Retrieves the current network time for the specified NTP request context, applying any
        /// client-specific rules as necessary.
        /// </summary>
        /// <remarks>If a client rule is found for the requesting client, the method simulates network
        /// delay according to the rule's response delay and applies any specified time offset or response metadata. The
        /// response may include additional fields such as leap indicator, stratum, or version number if defined by the
        /// client rule.</remarks>
        /// <param name="context">The NTP request context that contains information about the client making the request.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an NtpResponseDirective with the
        /// calculated timestamp and any applicable response metadata based on client rules.</returns>
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
