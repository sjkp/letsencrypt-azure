using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using LetsEncrypt.Azure.Core.V2.CertificateStores;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2
{
    public class AcmeClient
    {
        private readonly HttpClient http = new HttpClient();
        private readonly IDnsProvider dnsProvider;
        private readonly DnsLookupService dnsLookupService;
        private readonly ICertificateStore certificateStore;

        private readonly ILogger<AcmeClient> logger;

        public AcmeClient(IDnsProvider dnsProvider, DnsLookupService dnsLookupService, ICertificateStore certifcateStore, ILogger<AcmeClient> logger = null)
        {
            this.dnsProvider = dnsProvider;
            this.dnsLookupService = dnsLookupService;
            this.certificateStore = certifcateStore;
            this.logger = logger ?? NullLogger<AcmeClient>.Instance;
        }

        /// <summary>
        /// Request a certificate from lets encrypt using the DNS challenge, placing the challenge record in Azure DNS.
        /// The certifiacte is not assigned, but just returned.
        /// </summary>
        /// <param name="azureDnsEnvironment"></param>
        /// <param name="acmeConfig"></param>
        /// <returns></returns>
        public async Task<CertificateInstallModel> RequestDnsChallengeCertificate(IAcmeDnsRequest acmeConfig)
        {
            logger.LogInformation("Starting request DNS Challenge certificate for {AcmeEnvironment} and {Email}", acmeConfig.AcmeEnvironment.BaseUri, acmeConfig.RegistrationEmail);
            var acmeContext = await GetOrCreateAcmeContext(acmeConfig.AcmeEnvironment.BaseUri, acmeConfig.RegistrationEmail);
            var idn = new IdnMapping();

            var orderHosts = (from host in acmeConfig.Hosts
                              let asciiHost = idn.GetAscii(host)
                              let asciiDomain = asciiHost.StartsWith("*.")
                                              ? asciiHost.Substring(2)
                                              : asciiHost
                              select (Host: asciiHost, Domain: asciiDomain))
                             .ToImmutableArray();
            var order = await acmeContext.NewOrder(orderHosts.Select(h => h.Host).ToImmutableArray());
            var authorizations = (await order.Authorizations()).ToImmutableArray();
            var tasks = new List<Task>(authorizations.Length);
            var dnsTxts = new List<string>(authorizations.Length);
            // TODO: Consider parallelizing
            for (var i = 0; i < authorizations.Length; i++) /*tasks.Add(Task.Factory.StartNew(async state =>*/
            {
                //var (authorization, host) = (Tuple<IAuthorizationContext, string>)state;
                var (authorization, zoneName) = (authorizations[i], orderHosts[i].Domain);
                var challenge = await authorization.Dns();
                var dnsTxt = acmeContext.AccountKey.DnsTxt(challenge.Token);
                logger.LogInformation("Got DNS challenge token {Token}", dnsTxt);

                ///add dns entry
                await this.dnsProvider.PersistChallenge(zoneName, "_acme-challenge", dnsTxt);
                await Task.Delay(500);

                if (!(await this.dnsLookupService.Exists(zoneName, dnsTxt, this.dnsProvider.MinimumTtl)))
                {
                    throw new TimeoutException($"Unable to validate that _acme-challenge was stored in txt _acme-challenge record after {this.dnsProvider.MinimumTtl} seconds");
                }

                Challenge chalResp = await challenge.Validate();
                while (chalResp.Status == ChallengeStatus.Pending || chalResp.Status == ChallengeStatus.Processing)
                {
                    logger.LogInformation("Dns challenge response status {ChallengeStatus} more info at {ChallengeStatusUrl} retrying in 5 sec", chalResp.Status, chalResp.Url.ToString());
                    await Task.Delay(5000);
                    chalResp = await challenge.Resource();
                }

                logger.LogInformation("Finished validating dns challenge token, response was {ChallengeStatus} more info at {ChallengeStatusUrl}", chalResp.Status, chalResp.Url);
            }/*, Tuple.Create(authorizations[i], orderHosts[i].BaseHost), CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default).Unwrap());
            await Task.WhenAll(tasks);
            tasks.Clear()*/;

            var privateKey = await GetOrCreateKey(acmeConfig.AcmeEnvironment.BaseUri, acmeConfig.Hosts);
            var cert = await order.Generate(new Certes.CsrInfo
            {
                CountryName = acmeConfig.CsrInfo.CountryName,
                State = acmeConfig.CsrInfo.State,
                Locality = acmeConfig.CsrInfo.Locality,
                Organization = acmeConfig.CsrInfo.Organization,
                OrganizationUnit = acmeConfig.CsrInfo.OrganizationUnit,
                CommonName = acmeConfig.CsrInfo.CommonName,
            }, privateKey);

            var certPem = cert.ToPem();

            string hostsPlusSeparated = GetHostsPlusSeparated(acmeConfig.Hosts);
            var pfxBuilder = cert.ToPfx(privateKey);
            var pfx = pfxBuilder.Build(hostsPlusSeparated, acmeConfig.PFXPassword);

            for (var i = 0; i < dnsTxts.Count; i++)
            {
                tasks.Add(this.dnsProvider.Cleanup(orderHosts[i].Domain, dnsTxts[i]));
            }
            await Task.WhenAll(tasks);
            tasks.Clear();

            return new CertificateInstallModel()
            {
                CertificateInfo = new CertificateInfo()
                {
#pragma warning disable DF0100 // Marks return values that hides the IDisposable implementation of return value.
                    Certificate = new X509Certificate2(pfx, acmeConfig.PFXPassword, X509KeyStorageFlags.DefaultKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable),
#pragma warning restore DF0100 // Marks return values that hides the IDisposable implementation of return value.
                    Name = $"{acmeConfig.Hosts} {DateTime.Now}",
                    Password = acmeConfig.PFXPassword,
                    PfxCertificate = pfx
                },
                Hosts = acmeConfig.Hosts
            };
        }

        internal static string GetHostsPlusSeparated(ImmutableArray<string> hosts)
         => string.Join("+", hosts.Select(h => h.StartsWith("*") ? h.Substring(1) : h));

        private async Task<IKey> GetOrCreateKey(Uri acmeDirectory, ImmutableArray<string> hosts)
        {
            string hostsPlusSeparated = GetHostsPlusSeparated(hosts);
            string secretName = $"privatekey-{hostsPlusSeparated}--{acmeDirectory.Host}";
            var key = await this.certificateStore.GetSecret(secretName);
            if (string.IsNullOrEmpty(key))
            {
                var privatekey = KeyFactory.NewKey(KeyAlgorithm.RS256);
                await this.certificateStore.SaveSecret(secretName, privatekey.ToPem());
                return privatekey;
            }

            return KeyFactory.FromPem(key);
        }

        private async Task<AcmeContext> GetOrCreateAcmeContext(Uri acmeDirectoryUri, string email)
        {
            AcmeContext acme = null;
            string filename = $"account{email}--{acmeDirectoryUri.Host}";
            var secret = await this.certificateStore.GetSecret(filename);
            if (string.IsNullOrEmpty(secret))
            {
                acme = new AcmeContext(acmeDirectoryUri);
                var account = acme.NewAccount(email, true);

                // Save the account key for later use
                var pemKey = acme.AccountKey.ToPem();
                await certificateStore.SaveSecret(filename, pemKey);
                await Task.Delay(10000); //Wait a little before using the new account.
                acme = new AcmeContext(acmeDirectoryUri, acme.AccountKey, new AcmeHttpClient(acmeDirectoryUri, http));
            }
            else
            {
                var accountKey = KeyFactory.FromPem(secret);
                acme = new AcmeContext(acmeDirectoryUri, accountKey, new AcmeHttpClient(acmeDirectoryUri, http));
            }

            return acme;
        }
    }
}
