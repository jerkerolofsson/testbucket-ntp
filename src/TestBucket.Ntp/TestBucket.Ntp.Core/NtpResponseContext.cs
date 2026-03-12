using TestBucket.Ntp.Core.Protocol;

namespace TestBucket.Ntp.Core
{
    public class NtpResponseContext
    {
        /// <summary>
        /// Calculated time based on the NTP response.
        /// This is the time that the client would calculate as the current time after processing the NTP response,
        /// computed as T4 + ClockOffset where ClockOffset = ((T2-T1) + (T3-T4)) / 2.
        /// </summary>
        public required DateTimeOffset CalculatedTime { get; set; }

        /// <summary>
        /// The server transmit timestamp (T3) reported in the NTP response packet.
        /// </summary>
        public required DateTimeOffset ServerTransmitTime { get; set; }

        /// <summary>
        /// The estimated clock offset between the client and the server.
        /// Computed as ((T2-T1) + (T3-T4)) / 2, where T1/T4 are client send/receive times
        /// and T2/T3 are the server receive/transmit timestamps.
        /// </summary>
        public TimeSpan ClockOffset { get; set; }

        /// <summary>
        /// The round-trip network delay between the client and the server.
        /// Computed as (T4-T1) - (T3-T2).
        /// </summary>
        public TimeSpan RoundTripDelay { get; set; }

        public required NtpPacket? Packet { get; set; }
        public byte[]? RawBytes { get; set; }

        /// <summary>
        /// T1
        /// </summary>
        public DateTimeOffset ClientTransmitTime { get; set; }

        /// <summary>
        /// T2
        /// </summary>
        public DateTimeOffset ServerReceiveTime { get; set; }
        public DateTimeOffset ClientReceiveTime { get; set; }
    }
}
