using LetsEncrypt.Azure.Core.V2;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Letsencrypt.Azure.Core.Test
{
    [TestClass]
    public class UnoEuroDnsProviderTest
    {
        [TestMethod]
        public async Task CreateRecord()
        {
            var config = new ConfigurationBuilder()
              .AddUserSecrets<UnoEuroDnsProviderTest>()
              .Build();

            string domain = config["domain"];
            var dnsProvider = new UnoEuroDnsProvider(new UnoEuroDnsSettings()
            {
                AccountName = config["accountName"],
                ApiKey = config["apiKey"],
                Domain = domain
            });
            //Test create new
            await dnsProvider.PersistChallenge(zoneName: domain, recordSetName: "_acme-challenge", recordValue: Guid.NewGuid().ToString());
            //Test Update existing
            await dnsProvider.PersistChallenge(zoneName: domain, recordSetName: "_acme-challenge", recordValue: Guid.NewGuid().ToString());
            //Test clean up
            await dnsProvider.Cleanup(domain, "_acme-challenge");

        }

        [TestMethod]
        public async Task UnoEuroDnsTest()
        {
            var service = TestHelper.UnoEuroDnsProvider;

            const string Domain = "tiimo.dk";
            const string HostName = "*." + Domain;

            var id = Guid.NewGuid().ToString();
            await service.PersistChallenge(zoneName: Domain, recordSetName: "_acme-challenge", recordValue: id);

            var exists = await new DnsLookupService().Exists(HostName, id, service.MinimumTtl);
            Assert.IsTrue(exists);

            await service.Cleanup(Domain, "_acme-challenge");
        }


    }
}
