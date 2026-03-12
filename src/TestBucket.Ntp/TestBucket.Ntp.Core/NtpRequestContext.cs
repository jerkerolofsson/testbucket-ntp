using System.Net;
using TestBucket.Ntp.Core.Protocol;

namespace TestBucket.Ntp.Core
{
    public class NtpRequestContext
    {
        /// <summary>
        /// The IP address of the client that made the NTP request
        /// </summary>
        public required IPAddress ClientAddress { get; set; }
    }
}
