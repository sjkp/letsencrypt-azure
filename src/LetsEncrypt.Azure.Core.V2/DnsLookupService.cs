using DnsClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2
{
    public class DnsLookupService
    {
        private readonly ILogger<DnsLookupService> logger;

        public DnsLookupService(ILogger<DnsLookupService> logger = null)
        {
            this.logger = logger ?? NullLogger<DnsLookupService>.Instance;
        }

        public async Task<bool> Exists(string zoneName, string dnsTxt, int timeout = 60)
        {
            logger.LogInformation("Starting dns precheck validation for hostname: {HostName} challenge: {Challenge} and timeout {Timeout}", zoneName, dnsTxt, timeout);
            var idn = new IdnMapping();
            zoneName = idn.GetAscii(zoneName);
            var dnsClient = GetDnsClient(zoneName);
            var startTime = DateTime.UtcNow;
            bool result = false;
            do
            {
                var servers = dnsClient.NameServers.Select(s => s.Endpoint.Address).ToImmutableArray();
                logger.LogInformation("Validating dns challenge exists on name servers {NameServers}", string.Join(", ", servers));
                var dnsRes = dnsClient.QueryServer(servers, $"_acme-challenge.{zoneName}", QueryType.TXT);
                result = dnsRes.Answers.TxtRecords().FirstOrDefault()?.Text.Any(r => r == dnsTxt) ?? false;
                if (!result)
                {
                    logger.LogInformation("Challenge record missing, retrying again in 5 seconds");
                    await Task.Delay(5000);
                }

            } while (!result && (DateTime.UtcNow - startTime).TotalSeconds < timeout);

            return result;
        }

        private static LookupClient GetDnsClient(string zoneName)
        {
            LookupClient generalClient = new LookupClient();
            generalClient.UseCache = false;
            var ns = generalClient.Query(zoneName, QueryType.NS);
            var ip = ns.Answers.NsRecords().Select(s => generalClient.GetHostEntry(s.NSDName.Value));

            var nameServers = ip.SelectMany(i => i.AddressList)
                                .Where(s => s.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                                .ToArray();
            LookupClient dnsClient = new LookupClient(nameServers);
            dnsClient.UseCache = false;

            return dnsClient;
        }
    }
}
