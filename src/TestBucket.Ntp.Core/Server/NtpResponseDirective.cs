using TestBucket.Ntp.Core.Protocol;

namespace TestBucket.Ntp.Core.Server
{
    public class NtpResponseDirective
    {
        public required DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// If set, these bytes will be returned instead of a proper NTP response. This allows tests to simulate malformed responses or responses that don't follow the NTP protocol.
        /// </summary>
        public byte[]? ResponseBytes { get; set; }

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
