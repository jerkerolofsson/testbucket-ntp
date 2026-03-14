using System.Buffers.Binary;
using System.Net.Sockets;
using TestBucket.Ntp.Core.Server;

namespace TestBucket.Ntp.Core.Protocol
{
    /// <summary>
    /// Parses and creates NTP packets according to RFC4330
    /// </summary>
    public static class NtpPacketParser
    {
        private const int MinPacketSize = 48;
        private const ulong NtpEpochOffset = 2208988800ul; // Seconds between 1900 and 1970

        /// <summary>
        /// Parses an NTP packet from a byte array
        /// </summary>
        public static NtpPacket? ParsePacket(byte[] data)
        {
            if (data == null || data.Length < MinPacketSize)
            {
                return null;
            }

            var packet = new NtpPacket();

            // Byte 0: LI (2 bits), VN (3 bits), Mode (3 bits)
            packet.LeapIndicator = (byte)((data[0] >> 6) & 0x03);
            packet.VersionNumber = (byte)((data[0] >> 3) & 0x07);
            packet.Mode = (byte)(data[0] & 0x07);

            // Byte 1: Stratum
            packet.Stratum = data[1];

            // Byte 2: Poll Interval
            packet.PollInterval = data[2];

            // Byte 3: Precision
            packet.Precision = (sbyte)data[3];

            // Bytes 4-7: Root Delay
            packet.RootDelay = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4, 4));

            // Bytes 8-11: Root Dispersion
            packet.RootDispersion = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(8, 4));

            // Bytes 12-15: Reference Identifier
            packet.ReferenceIdentifier = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(12, 4));

            // Bytes 16-23: Reference Timestamp
            packet.ReferenceTimestamp = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(16, 8));

            // Bytes 24-31: Originate Timestamp
            packet.OriginateTimestamp = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(24, 8));

            // Bytes 32-39: Receive Timestamp
            packet.ReceiveTimestamp = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(32, 8));

            // Bytes 40-47: Transmit Timestamp
            packet.TransmitTimestamp = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(40, 8));

            return packet;
        }

        /// <summary>
        /// Creates an NTP response packet
        /// </summary>
        public static byte[] CreateResponse(NtpPacket request, DateTimeOffset serverTime, NtpResponseDirective? responseDirective = null)
        {
            var response = new byte[MinPacketSize];

            byte versionNumber = responseDirective?.ResponseVersionNumber ?? request.VersionNumber;
            byte li = responseDirective?.ResponseLeapIndicator ?? NtpProtocolDefaults.LeapIndicator;
            byte mode = responseDirective?.ResponseMode ?? NtpProtocolDefaults.ResponseMode;

            // Byte 0: LI (2 bits), VN (3 bits), Mode (3 bits)
            // LI = 0 (no warning), VN = request version, Mode = 4 (server)
            response[0] = (byte)((li << 6) | (versionNumber << 3) | mode);

            // Byte 1: Stratum (use 2 for secondary reference)
            response[1] = responseDirective?.ResponseStratum ?? NtpProtocolDefaults.Stratum;

            // Byte 2: Poll Interval (copy from request if not overridden)
            response[2] = responseDirective?.ResponsePollInterval ?? request.PollInterval;

            // Byte 3: Precision
            response[3] = unchecked((byte)(responseDirective?.ResponsePrecision ?? NtpProtocolDefaults.Precision));

            // Bytes 4-7: Root Delay
            BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(4, 4), responseDirective?.ResponseRootDelay ?? NtpProtocolDefaults.RootDelay);

            // Bytes 8-11: Root Dispersion
            BinaryPrimitives.WriteUInt32BigEndian(response.AsSpan(8, 4), responseDirective?.ResponseRootDispersion ?? NtpProtocolDefaults.RootDispersion);

            // Bytes 12-15: Reference Identifier (use "LOCL" for local clock)
            response[12] = (byte)'L';
            response[13] = (byte)'O';
            response[14] = (byte)'C';
            response[15] = (byte)'L';
            if(responseDirective?.ResponseResourceIdentifier is not null && responseDirective.ResponseResourceIdentifier.Length == 4)
            {
                response[12] = responseDirective.ResponseResourceIdentifier[0];
                response[13] = responseDirective.ResponseResourceIdentifier[1];
                response[14] = responseDirective.ResponseResourceIdentifier[2];
                response[15] = responseDirective.ResponseResourceIdentifier[3];
            }

            // Bytes 16-23: Reference Timestamp (current time)
            var referenceTimestamp = DateTimeOffsetToNtpTimestamp(serverTime);
            BinaryPrimitives.WriteUInt64BigEndian(response.AsSpan(16, 8), referenceTimestamp);

            // Bytes 24-31: Originate Timestamp (copy from client's transmit timestamp)
            BinaryPrimitives.WriteUInt64BigEndian(response.AsSpan(24, 8), request.TransmitTimestamp);

            // Bytes 32-39: Receive Timestamp (time when request was received)
            BinaryPrimitives.WriteUInt64BigEndian(response.AsSpan(32, 8), referenceTimestamp);

            // Bytes 40-47: Transmit Timestamp (copy from request if not overridden)
            BinaryPrimitives.WriteUInt64BigEndian(response.AsSpan(40, 8), responseDirective?.ResponseTransmitTimestamp ?? referenceTimestamp);

            return response;
        }

        /// <summary>
        /// Converts a DateTimeOffset to NTP timestamp format (seconds since 1900)
        /// </summary>
        private static ulong DateTimeOffsetToNtpTimestamp(DateTimeOffset dateTime)
        {
            var unixSeconds = (ulong)dateTime.ToUnixTimeSeconds();
            var ntpSeconds = unixSeconds + NtpEpochOffset;

            var milliseconds = dateTime.Millisecond;
            var fraction = (ulong)((milliseconds / 1000.0) * 0x100000000L);

            return (ntpSeconds << 32) | fraction;
        }

        /// <summary>
        /// Validates that the request packet is a valid client request
        /// </summary>
        public static bool IsValidClientRequest(NtpPacket? packet)
        {
            if (packet == null)
            {
                return false;
            }

            // Must be a client request (mode 3) or symmetric active (mode 1)
            if (packet.Mode != 3 && packet.Mode != 1)
            {
                return false;
            }

            // Version should be 3 or 4
            if (packet.VersionNumber < 3 || packet.VersionNumber > 4)
            {
                return false;
            }

            return true;
        }

    }
}
