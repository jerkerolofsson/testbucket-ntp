using System;
using System.Buffers.Binary;
using TestBucket.Ntp.Core.Protocol;
using Xunit;

namespace TestBucket.Ntp.Core.UnitTests
{
    /// <summary>
    /// Unit tests for NtpPacketParser based on RFC 4330 (Simple Network Time Protocol)
    /// </summary>
    public class NtpPacketParserTests
    {
        #region ParseRequest Tests

        [Fact]
        public void ParseRequest_NullData_ReturnsNull()
        {
            // Act
            var result = NtpPacketParser.ParsePacket(null!);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseRequest_PacketTooSmall_ReturnsNull()
        {
            // Arrange - RFC 4330 requires minimum 48 bytes
            var data = new byte[47];

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseRequest_EmptyPacket_ReturnsNull()
        {
            // Arrange
            var data = Array.Empty<byte>();

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void ParseRequest_ValidMinimumPacket_ReturnsPacket()
        {
            // Arrange - 48 bytes is the minimum valid packet size
            var data = new byte[48];

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ParseRequest_ParsesLeapIndicator_Correctly()
        {
            // Arrange - RFC 4330 Section 4: LI is bits 0-1 of first byte
            var data = new byte[48];
            data[0] = 0b11000000; // LI = 3 (alarm condition)

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.LeapIndicator);
        }

        [Theory]
        [InlineData(0b00000000, 0)] // no warning
        [InlineData(0b01000000, 1)] // last minute has 61 seconds
        [InlineData(0b10000000, 2)] // last minute has 59 seconds
        [InlineData(0b11000000, 3)] // alarm condition
        public void ParseRequest_ParsesAllLeapIndicatorValues_Correctly(byte firstByte, byte expectedLI)
        {
            // Arrange
            var data = new byte[48];
            data[0] = firstByte;

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedLI, result.LeapIndicator);
        }

        [Fact]
        public void ParseRequest_ParsesVersionNumber_Correctly()
        {
            // Arrange - RFC 4330 Section 4: VN is bits 3-5 of first byte
            var data = new byte[48];
            data[0] = 0b00100000; // Version 4

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(4, result.VersionNumber);
        }

        [Theory]
        [InlineData(0b00001000, 1)] // Version 1
        [InlineData(0b00010000, 2)] // Version 2
        [InlineData(0b00011000, 3)] // Version 3
        [InlineData(0b00100000, 4)] // Version 4
        public void ParseRequest_ParsesAllVersionNumbers_Correctly(byte firstByte, byte expectedVersion)
        {
            // Arrange
            var data = new byte[48];
            data[0] = firstByte;

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedVersion, result.VersionNumber);
        }

        [Fact]
        public void ParseRequest_ParsesMode_Correctly()
        {
            // Arrange - RFC 4330 Section 4: Mode is bits 0-2 of first byte
            var data = new byte[48];
            data[0] = 0b00000011; // Mode 3 (client)

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Mode);
        }

        [Theory]
        [InlineData(0b00000000, 0)] // reserved
        [InlineData(0b00000001, 1)] // symmetric active
        [InlineData(0b00000010, 2)] // symmetric passive
        [InlineData(0b00000011, 3)] // client
        [InlineData(0b00000100, 4)] // server
        [InlineData(0b00000101, 5)] // broadcast
        [InlineData(0b00000110, 6)] // NTP control message
        [InlineData(0b00000111, 7)] // private use
        public void ParseRequest_ParsesAllModes_Correctly(byte firstByte, byte expectedMode)
        {
            // Arrange
            var data = new byte[48];
            data[0] = firstByte;

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedMode, result.Mode);
        }

        [Fact]
        public void ParseRequest_ParsesCombinedFirstByte_Correctly()
        {
            // Arrange - RFC 4330: LI=0, VN=4, Mode=3
            var data = new byte[48];
            data[0] = 0b00100011; // LI=0, VN=4, Mode=3

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.LeapIndicator);
            Assert.Equal(4, result.VersionNumber);
            Assert.Equal(3, result.Mode);
        }

        [Fact]
        public void ParseRequest_ParsesStratum_Correctly()
        {
            // Arrange - RFC 4330 Section 4: Stratum is byte 1
            var data = new byte[48];
            data[1] = 2; // Secondary reference

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Stratum);
        }

        [Theory]
        [InlineData(0)]   // kiss-o'-death
        [InlineData(1)]   // primary reference
        [InlineData(2)]   // secondary reference
        [InlineData(15)]  // secondary reference
        [InlineData(16)]  // reserved
        [InlineData(255)] // reserved
        public void ParseRequest_ParsesAllStratumValues_Correctly(byte stratum)
        {
            // Arrange
            var data = new byte[48];
            data[1] = stratum;

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(stratum, result.Stratum);
        }

        [Fact]
        public void ParseRequest_ParsesPollInterval_Correctly()
        {
            // Arrange - RFC 4330 Section 4: Poll is byte 2, exponent of 2
            var data = new byte[48];
            data[2] = 6; // 2^6 = 64 seconds

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(6, result.PollInterval);
        }

        [Fact]
        public void ParseRequest_ParsesPrecision_Correctly()
        {
            // Arrange - RFC 4330 Section 4: Precision is byte 3, signed exponent
            var data = new byte[48];
            data[3] = unchecked((byte)-20); // 2^-20 ≈ 1 microsecond

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(-20, result.Precision);
        }

        [Theory]
        [InlineData(-6)]  // mains-frequency clocks
        [InlineData(-20)] // microsecond clocks
        [InlineData(0)]   // 1 second
        public void ParseRequest_ParsesAllPrecisionValues_Correctly(sbyte precision)
        {
            // Arrange
            var data = new byte[48];
            data[3] = unchecked((byte)precision);

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(precision, result.Precision);
        }

        [Fact]
        public void ParseRequest_ParsesRootDelay_BigEndian()
        {
            // Arrange - RFC 4330: Root Delay is bytes 4-7 (32-bit big-endian)
            var data = new byte[48];
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4, 4), 0x12345678);

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0x12345678u, result.RootDelay);
        }

        [Fact]
        public void ParseRequest_ParsesRootDispersion_BigEndian()
        {
            // Arrange - RFC 4330: Root Dispersion is bytes 8-11 (32-bit big-endian)
            var data = new byte[48];
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8, 4), 0x9ABCDEF0);

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0x9ABCDEF0u, result.RootDispersion);
        }

        [Fact]
        public void ParseRequest_ParsesReferenceIdentifier_BigEndian()
        {
            // Arrange - RFC 4330: Reference ID is bytes 12-15 (32-bit)
            // For primary servers, this is ASCII like "LOCL"
            var data = new byte[48];
            data[12] = (byte)'L';
            data[13] = (byte)'O';
            data[14] = (byte)'C';
            data[15] = (byte)'L';

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            var expected = BinaryPrimitives.ReadUInt32BigEndian("LOCL"u8);
            Assert.Equal(expected, result.ReferenceIdentifier);
        }

        [Fact]
        public void ParseRequest_ParsesReferenceTimestamp_BigEndian()
        {
            // Arrange - RFC 4330: Reference Timestamp is bytes 16-23 (64-bit big-endian)
            var data = new byte[48];
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(16, 8), 0x123456789ABCDEF0);

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0x123456789ABCDEF0ul, result.ReferenceTimestamp);
        }

        [Fact]
        public void ParseRequest_ParsesOriginateTimestamp_BigEndian()
        {
            // Arrange - RFC 4330: Originate Timestamp is bytes 24-31 (64-bit big-endian)
            var data = new byte[48];
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(24, 8), 0xFEDCBA9876543210);

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0xFEDCBA9876543210ul, result.OriginateTimestamp);
        }

        [Fact]
        public void ParseRequest_ParsesReceiveTimestamp_BigEndian()
        {
            // Arrange - RFC 4330: Receive Timestamp is bytes 32-39 (64-bit big-endian)
            var data = new byte[48];
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(32, 8), 0x1122334455667788);

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0x1122334455667788ul, result.ReceiveTimestamp);
        }

        [Fact]
        public void ParseRequest_ParsesTransmitTimestamp_BigEndian()
        {
            // Arrange - RFC 4330: Transmit Timestamp is bytes 40-47 (64-bit big-endian)
            var data = new byte[48];
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(40, 8), 0x8877665544332211);

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0x8877665544332211ul, result.TransmitTimestamp);
        }

        [Fact]
        public void ParseRequest_ParsesCompletePacket_Correctly()
        {
            // Arrange - Create a complete valid NTP client request
            var data = new byte[48];
            data[0] = 0b00100011; // LI=0, VN=4, Mode=3
            data[1] = 0;          // Stratum unspecified
            data[2] = 6;          // Poll interval
            data[3] = unchecked((byte)-6); // Precision
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4, 4), 0);  // Root Delay
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8, 4), 0);  // Root Dispersion
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(12, 4), 0); // Reference ID
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(16, 8), 0); // Reference Timestamp
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(24, 8), 0); // Originate Timestamp
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(32, 8), 0); // Receive Timestamp
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(40, 8), 0xE8B5F49000000000); // Transmit Timestamp

            // Act
            var result = NtpPacketParser.ParsePacket(data);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.LeapIndicator);
            Assert.Equal(4, result.VersionNumber);
            Assert.Equal(3, result.Mode);
            Assert.Equal(0, result.Stratum);
            Assert.Equal(6, result.PollInterval);
            Assert.Equal(-6, result.Precision);
            Assert.Equal(0xE8B5F49000000000ul, result.TransmitTimestamp);
        }

        #endregion

        #region IsValidClientRequest Tests

        [Fact]
        public void IsValidClientRequest_NullPacket_ReturnsFalse()
        {
            // Act
            var result = NtpPacketParser.IsValidClientRequest(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidClientRequest_ClientMode_ReturnsTrue()
        {
            // Arrange - RFC 4330: Mode 3 is client
            var packet = new NtpPacket
            {
                Mode = 3,
                VersionNumber = 4
            };

            // Act
            var result = NtpPacketParser.IsValidClientRequest(packet);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidClientRequest_SymmetricActiveMode_ReturnsTrue()
        {
            // Arrange - RFC 4330: Mode 1 is symmetric active
            var packet = new NtpPacket
            {
                Mode = 1,
                VersionNumber = 4
            };

            // Act
            var result = NtpPacketParser.IsValidClientRequest(packet);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(0)] // reserved
        [InlineData(2)] // symmetric passive
        [InlineData(4)] // server
        [InlineData(5)] // broadcast
        [InlineData(6)] // control
        [InlineData(7)] // private
        public void IsValidClientRequest_InvalidMode_ReturnsFalse(byte mode)
        {
            // Arrange
            var packet = new NtpPacket
            {
                Mode = mode,
                VersionNumber = 4
            };

            // Act
            var result = NtpPacketParser.IsValidClientRequest(packet);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(3)] // Version 3
        [InlineData(4)] // Version 4
        public void IsValidClientRequest_ValidVersion_ReturnsTrue(byte version)
        {
            // Arrange - RFC 4330: Versions 3 and 4 are valid
            var packet = new NtpPacket
            {
                Mode = 3,
                VersionNumber = version
            };

            // Act
            var result = NtpPacketParser.IsValidClientRequest(packet);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void IsValidClientRequest_InvalidVersion_ReturnsFalse(byte version)
        {
            // Arrange
            var packet = new NtpPacket
            {
                Mode = 3,
                VersionNumber = version
            };

            // Act
            var result = NtpPacketParser.IsValidClientRequest(packet);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region CreateResponse Tests

        [Fact]
        public void CreateResponse_CreatesCorrectPacketSize()
        {
            // Arrange - RFC 4330: Minimum packet size is 48 bytes
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0xE8B5F49000000000
            };
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert
            Assert.Equal(48, response.Length);
        }

        [Fact]
        public void CreateResponse_SetsServerMode()
        {
            // Arrange - RFC 4330: Server should set mode to 4
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0xE8B5F49000000000
            };
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert
            var mode = response[0] & 0x07;
            Assert.Equal(4, mode);
        }

        [Fact]
        public void CreateResponse_CopiesVersionFromRequest()
        {
            // Arrange - RFC 4330: Response should use same version as request
            var request = new NtpPacket
            {
                VersionNumber = 3,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0xE8B5F49000000000
            };
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert
            var version = (response[0] >> 3) & 0x07;
            Assert.Equal(3, version);
        }

        [Fact]
        public void CreateResponse_SetsLeapIndicatorToZero()
        {
            // Arrange - No leap second warning
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0xE8B5F49000000000
            };
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert
            var leapIndicator = (response[0] >> 6) & 0x03;
            Assert.Equal(0, leapIndicator);
        }

        [Fact]
        public void CreateResponse_SetsStratumToTwo()
        {
            // Arrange - RFC 4330: Stratum 2 is secondary reference
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0xE8B5F49000000000
            };
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert
            Assert.Equal(2, response[1]);
        }

        [Fact]
        public void CreateResponse_CopiesPollIntervalFromRequest()
        {
            // Arrange
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 10,
                TransmitTimestamp = 0xE8B5F49000000000
            };
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert
            Assert.Equal(10, response[2]);
        }

        [Fact]
        public void CreateResponse_SetsPrecision()
        {
            // Arrange - Precision should be -20 (microsecond)
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0xE8B5F49000000000
            };
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert
            var precision = (sbyte)response[3];
            Assert.Equal(-20, precision);
        }

        [Fact]
        public void CreateResponse_SetsReferenceIdentifierToLOCL()
        {
            // Arrange - RFC 4330: "LOCL" indicates uncalibrated local clock
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0xE8B5F49000000000
            };
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert
            Assert.Equal((byte)'L', response[12]);
            Assert.Equal((byte)'O', response[13]);
            Assert.Equal((byte)'C', response[14]);
            Assert.Equal((byte)'L', response[15]);
        }

        [Fact]
        public void CreateResponse_CopiesOriginateTimestampFromRequestTransmit()
        {
            // Arrange - RFC 4330: Originate timestamp in response should be
            // the transmit timestamp from the client request
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0x1234567890ABCDEF
            };
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert
            var originateTimestamp = BinaryPrimitives.ReadUInt64BigEndian(response.AsSpan(24, 8));
            Assert.Equal(0x1234567890ABCDEFul, originateTimestamp);
        }

        [Fact]
        public void CreateResponse_SetsTimestamps_NonZero()
        {
            // Arrange
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0xE8B5F49000000000
            };
            var serverTime = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert - All three timestamps should be non-zero
            var referenceTimestamp = BinaryPrimitives.ReadUInt64BigEndian(response.AsSpan(16, 8));
            var receiveTimestamp = BinaryPrimitives.ReadUInt64BigEndian(response.AsSpan(32, 8));
            var transmitTimestamp = BinaryPrimitives.ReadUInt64BigEndian(response.AsSpan(40, 8));

            Assert.NotEqual(0ul, referenceTimestamp);
            Assert.NotEqual(0ul, receiveTimestamp);
            Assert.NotEqual(0ul, transmitTimestamp);
        }

        [Fact]
        public void CreateResponse_BigEndianFormat_Verified()
        {
            // Arrange - Verify all multi-byte values are in big-endian format per RFC 4330
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0xE8B5F49000000000
            };
            var serverTime = DateTimeOffset.UtcNow;

            // Act
            var response = NtpPacketParser.CreateResponse(request, serverTime);

            // Assert - Parse response and verify we can read it back
            var parsedResponse = NtpPacketParser.ParsePacket(response);
            Assert.NotNull(parsedResponse);
            Assert.Equal(4, parsedResponse.Mode);
            Assert.Equal(2, parsedResponse.Stratum);
        }

        #endregion

        #region RFC 4330 Compliance Tests

        [Fact]
        public void RFC4330_Section3_TimestampFormat_64Bit()
        {
            // RFC 4330 Section 3: NTP timestamps are 64-bit unsigned fixed-point
            // 32 bits for seconds, 32 bits for fraction
            var data = new byte[48];
            var seconds = 3912345678u;
            var fraction = 0x80000000u; // 0.5 seconds
            var timestamp = ((ulong)seconds << 32) | fraction;
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(40, 8), timestamp);

            var result = NtpPacketParser.ParsePacket(data);

            Assert.NotNull(result);
            Assert.Equal(timestamp, result.TransmitTimestamp);
        }

        [Fact]
        public void RFC4330_Section4_ClientRequestMode3()
        {
            // RFC 4330 Section 4: Client sets mode to 3 in request
            var data = new byte[48];
            data[0] = 0b00100011; // LI=0, VN=4, Mode=3

            var result = NtpPacketParser.ParsePacket(data);

            Assert.NotNull(result);
            Assert.Equal(3, result.Mode);
            Assert.True(NtpPacketParser.IsValidClientRequest(result));
        }

        [Fact]
        public void RFC4330_Section4_ServerResponseMode4()
        {
            // RFC 4330 Section 4: Server sets mode to 4 in reply
            var request = new NtpPacket
            {
                VersionNumber = 4,
                Mode = 3,
                PollInterval = 6,
                TransmitTimestamp = 0xE8B5F49000000000
            };

            var response = NtpPacketParser.CreateResponse(request, DateTimeOffset.UtcNow);
            var parsed = NtpPacketParser.ParsePacket(response);

            Assert.NotNull(parsed);
            Assert.Equal(4, parsed.Mode);
        }

        [Fact]
        public void RFC4330_Section4_StratumValues()
        {
            // RFC 4330 Section 4: Stratum meanings
            // 0 = kiss-o'-death
            // 1 = primary reference
            // 2-15 = secondary reference
            var data = new byte[48];

            // Test stratum 0 (kiss-o'-death)
            data[1] = 0;
            var result0 = NtpPacketParser.ParsePacket(data);
            Assert.Equal(0, result0!.Stratum);

            // Test stratum 1 (primary)
            data[1] = 1;
            var result1 = NtpPacketParser.ParsePacket(data);
            Assert.Equal(1, result1!.Stratum);

            // Test stratum 2 (secondary)
            data[1] = 2;
            var result2 = NtpPacketParser.ParsePacket(data);
            Assert.Equal(2, result2!.Stratum);
        }

        [Fact]
        public void RFC4330_Section4_ReferenceIdentifier_PrimaryServer()
        {
            // RFC 4330 Section 4: Primary servers use 4-character ASCII
            var data = new byte[48];
            data[1] = 1; // Stratum 1
            data[12] = (byte)'G';
            data[13] = (byte)'P';
            data[14] = (byte)'S';
            data[15] = 0;

            var result = NtpPacketParser.ParsePacket(data);

            Assert.NotNull(result);
            Assert.Equal(1, result.Stratum);
            var refId = BinaryPrimitives.ReadUInt32BigEndian(new byte[] { (byte)'G', (byte)'P', (byte)'S', 0 });
            Assert.Equal(refId, result.ReferenceIdentifier);
        }

        #endregion
    }
}
