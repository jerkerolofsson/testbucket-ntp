using System.Text;

namespace TestBucket.Ntp.Core.Protocol
{
    public class ExternalReferenceSource
    {
        public required byte[] Identifier { get; set; }
        public required string Description { get; set; }
        public string Name => Encoding.ASCII.GetString(Identifier).TrimEnd('\0');

        private static ExternalReferenceSource Create(string id, string description)
        {
            var bytes = new byte[4];
            Encoding.ASCII.GetBytes(id, 0, id.Length, bytes, 0);
            return new ExternalReferenceSource { Identifier = bytes, Description = description };
        }

        public static ExternalReferenceSource LOCL { get; } = Create("LOCL", "Uncalibrated local clock");
        public static ExternalReferenceSource CESM { get; } = Create("CESM", "Calibrated Cesium clock");
        public static ExternalReferenceSource RBDM { get; } = Create("RBDM", "Calibrated Rubidium clock");
        public static ExternalReferenceSource PPS  { get; } = Create("PPS",  "Calibrated quartz clock or other pulse-per-second source");
        public static ExternalReferenceSource IRIG { get; } = Create("IRIG", "Inter-Range Instrumentation Group");
        public static ExternalReferenceSource ACTS { get; } = Create("ACTS", "NIST telephone modem service");
        public static ExternalReferenceSource USNO { get; } = Create("USNO", "USNO telephone modem service");
        public static ExternalReferenceSource PTB  { get; } = Create("PTB",  "PTB (Germany) telephone modem service");
        public static ExternalReferenceSource TDF  { get; } = Create("TDF",  "Allouis (France) Radio 164 kHz");
        public static ExternalReferenceSource DCF  { get; } = Create("DCF",  "Mainflingen (Germany) Radio 77.5 kHz");
        public static ExternalReferenceSource MSF  { get; } = Create("MSF",  "Rugby (UK) Radio 60 kHz");
        public static ExternalReferenceSource WWV  { get; } = Create("WWV",  "Ft. Collins (US) Radio 2.5, 5, 10, 15, 20 MHz");
        public static ExternalReferenceSource WWVB { get; } = Create("WWVB", "Boulder (US) Radio 60 kHz");
        public static ExternalReferenceSource WWVH { get; } = Create("WWVH", "Kauai Hawaii (US) Radio 2.5, 5, 10, 15 MHz");
        public static ExternalReferenceSource CHU  { get; } = Create("CHU",  "Ottawa (Canada) Radio 3330, 7335, 14670 kHz");
        public static ExternalReferenceSource LORC { get; } = Create("LORC", "LORAN-C radionavigation system");
        public static ExternalReferenceSource OMEG { get; } = Create("OMEG", "OMEGA radionavigation system");
        public static ExternalReferenceSource GPS  { get; } = Create("GPS",  "Global Positioning Service");

        public static IReadOnlyList<ExternalReferenceSource> All { get; } =
        [
            LOCL, CESM, RBDM, PPS, IRIG, ACTS, USNO, PTB,
            TDF, DCF, MSF, WWV, WWVB, WWVH, CHU, LORC, OMEG, GPS
        ];
    }
}
