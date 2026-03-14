using System.Buffers.Binary;
using System.Net.Sockets;
using TestBucket.Ntp.Core.Protocol;

namespace TestBucket.Ntp.Core.Client
{
    public class NtpClient
    {
        private const int DefaultNtpPort = 123;
        private const int NtpPacketSize = 48;
        private const ulong NtpEpochOffset = 2208988800ul; // Seconds between 1900 and 1970
        private const int TimeoutMilliseconds = 5000;

        public async Task<NtpResponseContext> QueryAsync(string hostname)
        {
            int port = DefaultNtpPort;
            if(hostname.Contains(':'))
            {
                string[] parts = hostname.Split(':');
                hostname = parts[0];
                if (int.TryParse(parts[1], out int newPort))
                {
                    port = newPort;
                }
            }

            using var udpClient = new UdpClient();
            udpClient.Client.ReceiveTimeout = TimeoutMilliseconds;

            // Create NTP request packet
            var ntpData = new byte[NtpPacketSize];

            // Byte 0: LI (2 bits) = 0, VN (3 bits) = 4, Mode (3 bits) = 3 (client)
            ntpData[0] = 0x1B; // 0b00_100_011 = LI:0, VN:4, Mode:3

            // Connect to NTP server
            udpClient.Connect(hostname, port);

            // T1: record client transmit time and write into request as originate timestamp (bytes 40-47)
            var t1 = DateTimeOffset.UtcNow;
            BinaryPrimitives.WriteUInt64BigEndian(ntpData.AsSpan(40, 8), DateTimeOffsetToNtpTimestamp(t1));

            // Send request
            await udpClient.SendAsync(ntpData, ntpData.Length);

            // Receive response and record T4 (client receive time)
            var response = await udpClient.ReceiveAsync();
            var t4 = DateTimeOffset.UtcNow;

            if (response.Buffer.Length < NtpPacketSize)
            {
                throw new InvalidOperationException("Invalid NTP response received");
            }

            NtpPacket? responsePacket = NtpPacketParser.ParsePacket(response.Buffer);

            // T3: server transmit timestamp
            var transmitTimestamp = BinaryPrimitives.ReadUInt64BigEndian(response.Buffer.AsSpan(40, 8));
            var t3 = NtpTimestampToDateTimeOffset(transmitTimestamp);

            // T2: server receive timestamp
            var t2 = responsePacket != null
                ? NtpTimestampToDateTimeOffset(responsePacket.ReceiveTimestamp)
                : t3;

            // NTP offset = ((T2-T1) + (T3-T4)) / 2
            var offset = TimeSpan.FromTicks(((t2 - t1) + (t3 - t4)).Ticks / 2);

            // Round-trip delay = (T4-T1) - (T3-T2)
            var delay = (t4 - t1) - (t3 - t2);

            return new NtpResponseContext
            {
                Packet = responsePacket,
                ClientTransmitTime = t1,
                ServerReceiveTime = t2,
                ServerTransmitTime = t3,
                ClientReceiveTime = t4,
                CalculatedTime = t4 + offset,
                ClockOffset = offset,
                RoundTripDelay = delay,
                RawBytes = response.Buffer
            };
        }

        /// <summary>
        /// Returns the server time from the NTP response without applying any offset correction. 
        /// This is the raw time reported by the server.
        /// </summary>
        /// <param name="hostname"></param>
        /// <returns></returns>
        public async Task<DateTimeOffset> GetServerTimeAsync(string hostname)
        {
            NtpResponseContext response = await QueryAsync(hostname);
            return response.ServerTransmitTime;
        }

        private static DateTimeOffset NtpTimestampToDateTimeOffset(ulong ntpTimestamp)
        {
            // NTP timestamp format: 32-bit seconds + 32-bit fractional seconds
            var seconds = ntpTimestamp >> 32;
            var fraction = ntpTimestamp & 0xFFFFFFFF;

            // Convert NTP seconds (since 1900) to Unix seconds (since 1970)
            var unixSeconds = seconds - NtpEpochOffset;

            // Convert fraction to milliseconds
            var milliseconds = (fraction * 1000) / 0x100000000L;

            // Create DateTimeOffset
            var dateTime = DateTimeOffset.FromUnixTimeSeconds((long)unixSeconds)
                .AddMilliseconds(milliseconds);

            return dateTime;
        }

        private static ulong DateTimeOffsetToNtpTimestamp(DateTimeOffset dateTime)
        {
            var unixSeconds = (ulong)dateTime.ToUnixTimeSeconds();
            var ntpSeconds = unixSeconds + NtpEpochOffset;
            var fraction = (ulong)((dateTime.Millisecond / 1000.0) * 0x100000000L);
            return (ntpSeconds << 32) | fraction;
        }
    }
}
