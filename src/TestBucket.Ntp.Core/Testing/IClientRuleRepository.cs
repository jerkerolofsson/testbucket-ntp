using System.Net;

namespace TestBucket.Ntp.Core.Testing
{
    public interface IClientRuleRepository
    {
        /// <summary>
        /// Returns the client rule for a specific client IP address, or null if no rule exists for that IP address.
        /// </summary>
        /// <param name="clientIpAddress"></param>
        /// <returns></returns>
        Task<ClientRule?> GetClientRuleAsync(IPAddress clientIpAddress);

        /// <summary>
        /// Adds a client rule to the repo
        /// </summary>
        /// <param name="clientRule"></param>
        /// <returns></returns>
        Task AddClientRuleAsync(ClientRule clientRule);

        /// <summary>
        /// Removes a client rule
        /// </summary>
        /// <param name="clientRule"></param>
        /// <returns></returns>
        Task RemoveClientRuleAsync(ClientRule clientRule);

        /// <summary>
        /// Returns rules
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        Task<IReadOnlyList<ClientRule>> BrowseAsync(int offset, int count);
    }
}
