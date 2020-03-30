using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DnsClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace LetsEncrypt.Azure.Core.V2
{
    public class DnsLookupService
    {
        private readonly ILogger<DnsLookupService> logger;

        public DnsLookupService(ILogger<DnsLookupService> logger = null)
        {
            this.logger = logger ?? NullLogger<DnsLookupService>.Instance;
        }

        public async Task<bool> Exists(string hostname, string dnsTxt, int timeout = 60)
        {
            logger.LogInformation("Starting dns precheck validation for hostname: {HostName} challenge: {Challenge} and timeout {Timeout}", hostname, dnsTxt, timeout);
            var idn = new IdnMapping();
            hostname = idn.GetAscii(GetNoneWildcardDomain(hostname));
            var dnsClient = GetDnsClient(GetRootDomain(hostname));
            var startTime = DateTime.UtcNow;
            string queriedDns = "";
            //Lets encrypt checks a random authoritative server, thus we need to ensure that all respond with the challenge. 
            foreach (var ns in dnsClient.NameServers)
            {
                logger.LogInformation("Validating dns challenge exists on name server {NameServer}", ns.ToString());
                do
                {
                    var dnsRes = dnsClient.QueryServer(new[] { ns.Endpoint.Address }, $"_acme-challenge.{hostname}", QueryType.TXT);
                    queriedDns = dnsRes.Answers.TxtRecords().FirstOrDefault()?.Text.FirstOrDefault();
                    if (queriedDns != dnsTxt)
                    {
                        logger.LogInformation("Challenge record was {existingTxt} should have been {Challenge}, retrying again in 5 seconds", queriedDns, dnsTxt);
                        await Task.Delay(5000);
                    }

                } while (queriedDns != dnsTxt && (DateTime.UtcNow - startTime).TotalSeconds < timeout);
            }

            return queriedDns == dnsTxt;
        }

        private static LookupClient GetDnsClient(params string[] hostnames)
        {

            LookupClient generalClient = new LookupClient();
            LookupClient dnsClient = null;
            generalClient.UseCache = false;
            foreach (var hostname in hostnames)
            {
                var ns = generalClient.Query(hostname, QueryType.NS);
                var ip = ns.Answers.NsRecords().Select(s => generalClient.GetHostEntry(s.NSDName.Value));
            
                dnsClient = new LookupClient(ip.SelectMany(i => i.AddressList).Where(s => s.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToArray());
                dnsClient.UseCache = false;
                
            }

            return dnsClient;
        }

        public static string GetNoneWildcardDomain(string hostname)
        {
            return hostname.Replace("*.", "");
        }

        public static string GetNoneWildcardSubdomain(string hostname)
        {
            if (string.IsNullOrEmpty(hostname))
                return hostname;
            string rootDomain = GetRootDomain(hostname);
            string nonWildcardDomain = GetNoneWildcardDomain(hostname);
            return nonWildcardDomain.Replace($".{rootDomain}", "");
        }

        public static string GetRootDomain(string hostname)
        {
            if (string.IsNullOrEmpty(hostname))
                return hostname;
            string[] hostParts = hostname.Split('.');
            if (hostParts.Length < 2)
                return hostname;
            return String.Join(".", hostParts.Skip(Math.Max(0, hostParts.Length - 2)).Take(2));
        }
    }
}
