namespace TestBucket.Ntp.Core.Protocol
{
    internal static class NtpProtocolDefaults
    {
        public const byte LeapIndicator = 0;
        public const byte Stratum = 2;
        public const byte ResponseMode = 4;
        public const sbyte Precision = -20;
        public const uint RootDelay = 0;
        public const uint RootDispersion = 0;
    }
}
