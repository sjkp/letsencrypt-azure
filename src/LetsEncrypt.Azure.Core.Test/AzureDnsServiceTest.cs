using LetsEncrypt.Azure.Core.V2;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Letsencrypt.Azure.Core.Test
{
    [TestClass]
    public class AzureDnsServiceTest
    {
        private const string Domain = "ai4bots.com";
        private const string HostName = "*." + Domain;

        [TestMethod]
        public async Task AzureDnsTest()
        {
            var config = TestHelper.AzureDnsSettings;

            var service = new AzureDnsProvider(config);

            var id = Guid.NewGuid().ToString();
            await service.PersistChallenge(zoneName: Domain, recordSetName: "_acme-challenge", recordValue: id);


            var exists = await new DnsLookupService().Exists(HostName, id, service.MinimumTtl);
            Assert.IsTrue(exists);

            await service.Cleanup(Domain, "_acme-challenge");
        }
    }
}
