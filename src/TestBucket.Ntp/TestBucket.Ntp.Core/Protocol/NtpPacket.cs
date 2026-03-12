namespace TestBucket.Ntp.Core.Protocol
{
    /// <summary>
    /// Represents an NTP packet structure according to RFC4330
    /// </summary>
    public class NtpPacket
    {
        /// <summary>
        /// Leap Indicator (2 bits)
        /// 0 - no warning
        /// 1 - last minute has 61 seconds
        /// 2 - last minute has 59 seconds
        /// 3 - alarm condition (clock not synchronized)
        /// </summary>
        public byte LeapIndicator { get; set; }

        /// <summary>
        /// Version Number (3 bits) - NTP/SNTP version number
        /// </summary>
        public byte VersionNumber { get; set; }

        /// <summary>
        /// Mode (3 bits)
        /// 0 - reserved
        /// 1 - symmetric active
        /// 2 - symmetric passive
        /// 3 - client
        /// 4 - server
        /// 5 - broadcast
        /// 6 - reserved for NTP control message
        /// 7 - reserved for private use
        /// </summary>
        public byte Mode { get; set; }

        /// <summary>
        /// Stratum (8 bits) - Level of the local clock
        /// 0 - unspecified or unavailable
        /// 1 - primary reference (e.g., radio clock)
        /// 2-15 - secondary reference (via NTP or SNTP)
        /// 16-255 - reserved
        /// </summary>
        public byte Stratum { get; set; }

        /// <summary>
        /// Poll Interval (8 bits) - Maximum interval between successive messages
        /// </summary>
        public byte PollInterval { get; set; }

        /// <summary>
        /// Precision (8 bits) - Precision of the local clock
        /// </summary>
        public sbyte Precision { get; set; }

        /// <summary>
        /// Root Delay (32 bits) - Total round trip delay to the primary reference source
        /// </summary>
        public uint RootDelay { get; set; }

        /// <summary>
        /// Root Dispersion (32 bits) - Maximum error relative to the primary reference source
        /// </summary>
        public uint RootDispersion { get; set; }

        /// <summary>
        /// Reference Identifier (32 bits) - Identifies the particular reference source
        /// </summary>
        public uint ReferenceIdentifier { get; set; }

        /// <summary>
        /// Reference Timestamp (64 bits) - Time when the system clock was last set or corrected
        /// </summary>
        public ulong ReferenceTimestamp { get; set; }

        /// <summary>
        /// Originate Timestamp (64 bits) - Time at the client when the request departed for the server
        /// </summary>
        public ulong OriginateTimestamp { get; set; }

        /// <summary>
        /// Receive Timestamp (64 bits) - Time at the server when the request arrived from the client
        /// </summary>
        public ulong ReceiveTimestamp { get; set; }

        /// <summary>
        /// Transmit Timestamp (64 bits) - Time at the server when the response left for the client
        /// </summary>
        public ulong TransmitTimestamp { get; set; }
    }
}
