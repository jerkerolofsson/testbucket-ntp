using TestBucket.Ntp.Core.Protocol;

namespace TestBucket.Ntp.Core.Testing
{
    /// <summary>
    /// This defines an NTP rule for the client
    /// </summary>
    public class ClientRule
    {
        /// <summary>
        /// The IP address of the client that this rule will apply to
        /// </summary>
        public required string ClientIpAddress { get; set; }

        /// <summary>
        /// Adds a delay to the NTP response to simulate network latency or client processing time
        /// </summary>
        public TimeSpan ResponseDelay { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Adds an offset to the system time for the response
        /// </summary>
        public TimeSpan TimeOffset { get; set; }

        /// <summary>
        /// Resource identifier for the response
        /// </summary>
        public byte[] ResponseResourceIdentifier { get; set; } = [(byte)'L', (byte)'O', (byte)'C', (byte)'L'];
        
        /// <summary>
        /// Response mode
        /// </summary>
        public byte ResponseMode { get; set; } = NtpProtocolDefaults.ResponseMode;

        /// <summary>
        /// LI
        /// </summary>
        public byte ResponseLeapIndicator { get; set; } = NtpProtocolDefaults.LeapIndicator;

        /// <summary>
        /// Version for the response. If null the same version as the request will be used
        /// </summary>
        public byte? ResponseVersionNumber { get; set; }

        /// <summary>
        /// The response stratum
        /// </summary>
        public byte ResponseStratum { get; set; } = NtpProtocolDefaults.Stratum;

        /// <summary>
        /// Precision field for the response. Defaults to <see cref="NtpProtocolDefaults.Precision"/>.
        /// </summary>
        public sbyte ResponsePrecision { get; set; } = NtpProtocolDefaults.Precision;

        /// <summary>
        /// Root Delay field for the response. Defaults to <see cref="NtpProtocolDefaults.RootDelay"/>.
        /// </summary>
        public uint ResponseRootDelay { get; set; } = NtpProtocolDefaults.RootDelay;

        /// <summary>
        /// Root Dispersion field for the response. Defaults to <see cref="NtpProtocolDefaults.RootDispersion"/>.
        /// </summary>
        public uint ResponseRootDispersion { get; set; } = NtpProtocolDefaults.RootDispersion;

        /// <summary>
        /// Poll Interval field for the response. When null, the value is copied from the request.
        /// </summary>
        public byte? ResponsePollInterval { get; set; }

        /// <summary>
        /// Transmit Timestamp field for the response (NTP 64-bit format). When null, the value is copied from the request.
        /// </summary>
        public ulong? ResponseTransmitTimestamp { get; set; }
    }
}
