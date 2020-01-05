using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2.DnsProviders
{
    public class UnoEuroDnsProvider : IDnsProvider
    {
        private readonly HttpClient httpClient;

        public int MinimumTtl => 1200; //Minimum is 600, but their dns servers are quite slow at updating so give some extra time.

        public UnoEuroDnsProvider(UnoEuroDnsSettings settings)
        {
#pragma warning disable DF0020 // Marks undisposed objects assinged to a field, originated in an object creation.
            this.httpClient = new HttpClient();
#pragma warning restore DF0020 // Marks undisposed objects assinged to a field, originated in an object creation.
            httpClient.BaseAddress = new Uri($"https://api.unoeuro.com/1/{settings.AccountName}/{settings.ApiKey}/my/products/{settings.Domain}/dns/records/");
        }

        public async Task Cleanup(string zoneName, string recordSetName)
        {
            DnsRecord acmeChallengeRecord = await GetRecord(recordSetName);
            if (acmeChallengeRecord != null)
                using (var res = await this.httpClient.DeleteAsync($"{acmeChallengeRecord.record_id}"))
                {
#pragma warning disable DF0001 // Marks undisposed anonymous objects from method invocations.
                    res.EnsureSuccessStatusCode();
#pragma warning restore DF0001 // Marks undisposed anonymous objects from method invocations.
                }
        }

        public async Task PersistChallenge(string zoneName, string recordSetName, string recordValue)
        {
            DnsRecord acmeChallengeRecord = await GetRecord(recordSetName);

            if (acmeChallengeRecord != null)
            {
                //Update
                var update = new
                {
                    acmeChallengeRecord.type,
                    acmeChallengeRecord.ttl,
                    acmeChallengeRecord.name,
                    data = recordValue,
                    acmeChallengeRecord.priority
                };
                using (StringContent content = CreateRequestBody(update))
                using (var res = await httpClient.PutAsync($"{acmeChallengeRecord.record_id}", content))
                {
                    var s = res.Content.ReadAsStringAsync();
#pragma warning disable DF0001 // Marks undisposed anonymous objects from method invocations.
                    res.EnsureSuccessStatusCode();
#pragma warning restore DF0001 // Marks undisposed anonymous objects from method invocations.
                }
            }
            else
            {
                acmeChallengeRecord = new DnsRecord()
                {
                    ttl = MinimumTtl,
                    type = "TXT",
                    name = recordSetName,
                    data = recordValue,
                    priority = 0
                };
                using (StringContent content = CreateRequestBody(acmeChallengeRecord))
                using (var res = await httpClient.PostAsync("", content))
                {
#pragma warning disable DF0001 // Marks undisposed anonymous objects from method invocations.
                    res.EnsureSuccessStatusCode();
#pragma warning restore DF0001 // Marks undisposed anonymous objects from method invocations.
                }
            }
        }

        /// <summary>
        /// Create the request body, uno euro doesn't support charset in the content-type.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        private static StringContent CreateRequestBody(object body)
        {
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            content.Headers.ContentType.CharSet = string.Empty;
            return content;
        }

        private async Task<DnsRecord> GetRecord(string recordSetName)
        {
            var records = JsonConvert.DeserializeObject<DnsResponse>(await this.httpClient.GetStringAsync(""));

            var acmeChallengeRecord = records.records.FirstOrDefault(s => s.type == "TXT" && s.name == recordSetName);
            return acmeChallengeRecord;
        }

        public class DnsResponse
        {
            public DnsRecord[] records { get; set; }
            public string message { get; set; }
            public int status { get; set; }
        }

        public class DnsRecord
        {
            public int? record_id { get; set; }
            public string name { get; set; }
            public int ttl { get; set; }
            public string data { get; set; }
            public string type { get; set; }
            public int priority { get; set; }
        }
    }

    public class UnoEuroDnsSettings
    {
        public string AccountName { get; set; }
        public string ApiKey { get; set; }
        public string Domain { get; set; }
    }
}
