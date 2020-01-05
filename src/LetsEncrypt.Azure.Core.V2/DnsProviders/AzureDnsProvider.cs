using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Azure.Management.Dns.Fluent;
using Microsoft.Azure.Management.Dns.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2.DnsProviders
{
    public class AzureDnsProvider : IDnsProvider
    {
        private readonly IDnsManagementClient client;
        private readonly AzureDnsSettings settings;

        public AzureDnsProvider(AzureDnsSettings settings)
        {
            var restClient = AzureHelper.GetRestClient(settings.AzureServicePrincipal, settings.AzureSubscription);

#pragma warning disable DF0020 // Marks undisposed objects assinged to a field, originated in an object creation.
            this.client = new DnsManagementClient(restClient);
#pragma warning restore DF0020 // Marks undisposed objects assinged to a field, originated in an object creation.
            this.client.SubscriptionId = settings.AzureSubscription.SubscriptionId;
            this.settings = settings;
        }

        public int MinimumTtl => 60;

        public async Task Cleanup(string recordSetName, string zoneName)
        {
            var existingRecords = await SafeGetExistingRecords(recordSetName, zoneName);

            await this.client.RecordSets.DeleteAsync(this.settings.ResourceGroupName, zoneName, GetRelativeRecordSetName(recordSetName, zoneName), RecordType.TXT);
        }

        public async Task PersistChallenge(string zoneName, string recordSetName, string recordValue)
        {
            List<TxtRecord> records = new List<TxtRecord>()
            {
                new TxtRecord() { Value = new[] { recordValue } }
            };
            if ((await client.RecordSets.ListByTypeAsync(settings.ResourceGroupName, zoneName, RecordType.TXT)).Any())
            {
                var existingRecords = await SafeGetExistingRecords(recordSetName, zoneName);
                if (existingRecords != null)
                {
                    if (existingRecords.TxtRecords.Any(s => s.Value.Contains(recordValue)))
                    {
                        records = existingRecords.TxtRecords.ToList();
                    }
                    else
                    {
                        records.AddRange(existingRecords.TxtRecords);
                    }
                }
            }
            await this.client.RecordSets.CreateOrUpdateAsync(this.settings.ResourceGroupName, zoneName, GetRelativeRecordSetName(recordSetName, zoneName), RecordType.TXT, new RecordSetInner()
            {
                TxtRecords = records,
                TTL = MinimumTtl
            });
        }

        private string GetRelativeRecordSetName(string dnsTxt, string zoneName)
         => dnsTxt.Replace($".{zoneName}", "");

        private async Task<RecordSetInner> SafeGetExistingRecords(string recordSetName, string zoneName)
        {
            try
            {
                return await client.RecordSets.GetAsync(settings.ResourceGroupName, zoneName, GetRelativeRecordSetName(recordSetName, zoneName), RecordType.TXT);

            }
            catch (CloudException cex)
            {
                if (!cex.Message.StartsWith("The resource record "))
                {
                    throw;
                }
            }
            return null;
        }
    }
}
