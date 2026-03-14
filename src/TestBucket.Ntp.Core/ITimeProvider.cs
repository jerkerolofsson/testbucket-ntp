using TestBucket.Ntp.Core.Server;

namespace TestBucket.Ntp.Core
{
    public interface ITimeProvider
    {
        Task<NtpResponseDirective> GetTimeAsync(NtpRequestContext context, CancellationToken cancellationToken);
    }
}