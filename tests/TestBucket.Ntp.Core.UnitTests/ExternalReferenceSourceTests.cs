using System.Text;
using TestBucket.Ntp.Core.Protocol;
using Xunit;

namespace TestBucket.Ntp.Core.UnitTests
{
    /// <summary>
    /// Unit tests for ExternalReferenceSource
    /// </summary>
    public class ExternalReferenceSourceTests
    {
        #region Identifier Length Tests

        [Theory]
        [InlineData("LOCL")]
        [InlineData("CESM")]
        [InlineData("RBDM")]
        [InlineData("PPS")]
        [InlineData("IRIG")]
        [InlineData("ACTS")]
        [InlineData("USNO")]
        [InlineData("PTB")]
        [InlineData("TDF")]
        [InlineData("DCF")]
        [InlineData("MSF")]
        [InlineData("WWV")]
        [InlineData("WWVB")]
        [InlineData("WWVH")]
        [InlineData("CHU")]
        [InlineData("LORC")]
        [InlineData("OMEG")]
        [InlineData("GPS")]
        public void Identifier_IsAlways4Bytes(string name)
        {
            var source = GetByName(name);
            Assert.Equal(4, source.Identifier.Length);
        }

        #endregion

        #region Identifier Encoding Tests

        [Theory]
        [InlineData("LOCL")]
        [InlineData("CESM")]
        [InlineData("RBDM")]
        [InlineData("ACTS")]
        [InlineData("USNO")]
        [InlineData("WWVB")]
        [InlineData("WWVH")]
        [InlineData("LORC")]
        [InlineData("OMEG")]
        public void Identifier_4CharName_EncodesAllCharsAsASCII(string name)
        {
            var source = GetByName(name);
            var expected = Encoding.ASCII.GetBytes(name);
            Assert.Equal(expected, source.Identifier);
        }

        [Theory]
        [InlineData("PPS")]
        [InlineData("PTB")]
        [InlineData("TDF")]
        [InlineData("DCF")]
        [InlineData("MSF")]
        [InlineData("WWV")]
        [InlineData("CHU")]
        [InlineData("GPS")]
        public void Identifier_3CharName_PadsWithNullByte(string name)
        {
            var source = GetByName(name);
            Assert.Equal(0, source.Identifier[3]);
        }

        [Theory]
        [InlineData("PPS")]
        [InlineData("PTB")]
        [InlineData("TDF")]
        [InlineData("DCF")]
        [InlineData("MSF")]
        [InlineData("WWV")]
        [InlineData("CHU")]
        [InlineData("GPS")]
        public void Identifier_3CharName_FirstThreeBytesAreASCII(string name)
        {
            var source = GetByName(name);
            var expected = Encoding.ASCII.GetBytes(name);
            Assert.Equal(expected[0], source.Identifier[0]);
            Assert.Equal(expected[1], source.Identifier[1]);
            Assert.Equal(expected[2], source.Identifier[2]);
        }

        #endregion

        #region Description Tests

        [Fact]
        public void LOCL_HasCorrectDescription()
        {
            Assert.Equal("Uncalibrated local clock", ExternalReferenceSource.LOCL.Description);
        }

        [Fact]
        public void CESM_HasCorrectDescription()
        {
            Assert.Equal("Calibrated Cesium clock", ExternalReferenceSource.CESM.Description);
        }

        [Fact]
        public void RBDM_HasCorrectDescription()
        {
            Assert.Equal("Calibrated Rubidium clock", ExternalReferenceSource.RBDM.Description);
        }

        [Fact]
        public void GPS_HasCorrectDescription()
        {
            Assert.Equal("Global Positioning Service", ExternalReferenceSource.GPS.Description);
        }

        [Fact]
        public void WWV_HasCorrectDescription()
        {
            Assert.Equal("Ft. Collins (US) Radio 2.5, 5, 10, 15, 20 MHz", ExternalReferenceSource.WWV.Description);
        }

        #endregion

        #region Static Instance Tests

        [Fact]
        public void StaticInstances_AreNotNull()
        {
            Assert.NotNull(ExternalReferenceSource.LOCL);
            Assert.NotNull(ExternalReferenceSource.CESM);
            Assert.NotNull(ExternalReferenceSource.RBDM);
            Assert.NotNull(ExternalReferenceSource.PPS);
            Assert.NotNull(ExternalReferenceSource.IRIG);
            Assert.NotNull(ExternalReferenceSource.ACTS);
            Assert.NotNull(ExternalReferenceSource.USNO);
            Assert.NotNull(ExternalReferenceSource.PTB);
            Assert.NotNull(ExternalReferenceSource.TDF);
            Assert.NotNull(ExternalReferenceSource.DCF);
            Assert.NotNull(ExternalReferenceSource.MSF);
            Assert.NotNull(ExternalReferenceSource.WWV);
            Assert.NotNull(ExternalReferenceSource.WWVB);
            Assert.NotNull(ExternalReferenceSource.WWVH);
            Assert.NotNull(ExternalReferenceSource.CHU);
            Assert.NotNull(ExternalReferenceSource.LORC);
            Assert.NotNull(ExternalReferenceSource.OMEG);
            Assert.NotNull(ExternalReferenceSource.GPS);
        }

        [Fact]
        public void StaticInstances_ReturnSameReferenceOnRepeatedAccess()
        {
            Assert.Same(ExternalReferenceSource.GPS, ExternalReferenceSource.GPS);
            Assert.Same(ExternalReferenceSource.LOCL, ExternalReferenceSource.LOCL);
        }

        #endregion

        #region Helpers

        private static ExternalReferenceSource GetByName(string name) => name switch
        {
            "LOCL" => ExternalReferenceSource.LOCL,
            "CESM" => ExternalReferenceSource.CESM,
            "RBDM" => ExternalReferenceSource.RBDM,
            "PPS"  => ExternalReferenceSource.PPS,
            "IRIG" => ExternalReferenceSource.IRIG,
            "ACTS" => ExternalReferenceSource.ACTS,
            "USNO" => ExternalReferenceSource.USNO,
            "PTB"  => ExternalReferenceSource.PTB,
            "TDF"  => ExternalReferenceSource.TDF,
            "DCF"  => ExternalReferenceSource.DCF,
            "MSF"  => ExternalReferenceSource.MSF,
            "WWV"  => ExternalReferenceSource.WWV,
            "WWVB" => ExternalReferenceSource.WWVB,
            "WWVH" => ExternalReferenceSource.WWVH,
            "CHU"  => ExternalReferenceSource.CHU,
            "LORC" => ExternalReferenceSource.LORC,
            "OMEG" => ExternalReferenceSource.OMEG,
            "GPS"  => ExternalReferenceSource.GPS,
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };

        #endregion
    }
}
