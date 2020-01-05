using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2.DnsProviders
{
    public interface IDnsProvider
    {
        Task PersistChallenge(string zoneName, string recordSetName, string recordValue);
        Task Cleanup(string recordSetName, string zoneName);

        /// <summary>
        /// The minimum ttl value in seconds, that the provider supports.
        /// </summary>
        int MinimumTtl { get; }
    }
}