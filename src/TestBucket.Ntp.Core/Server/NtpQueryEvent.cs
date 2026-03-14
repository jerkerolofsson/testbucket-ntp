using System.Net;
using TestBucket.Ntp.Core.Protocol;

namespace TestBucket.Ntp.Core.Server
{
    public class NtpQueryEvent
    {
        public DateTimeOffset Created { get; } = DateTimeOffset.UtcNow;
        public required IPAddress ClientAddress { get; set; }
        public required NtpPacket Request { get; set; }
        public required byte[] RequestData { get; set; }
        public required byte[] ResponseData { get; set; }
        public NtpPacket? Response { get; set; }
    }
}
