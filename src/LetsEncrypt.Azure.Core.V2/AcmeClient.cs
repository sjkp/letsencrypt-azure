﻿using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using LetsEncrypt.Azure.Core.V2.CertificateStores;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2
{
    public class AcmeClient
    {
        private readonly IDnsProvider dnsProvider;
        private readonly DnsLookupService dnsLookupService;
        private readonly ICertificateStore certificateStore;

        private readonly ILogger<AcmeClient> logger;

        public AcmeClient(IDnsProvider dnsProvider, DnsLookupService dnsLookupService, ICertificateStore certifcateStore, ILogger<AcmeClient> logger = null)
        {
            this.dnsProvider = dnsProvider;
            this.dnsLookupService = dnsLookupService;
            this.certificateStore = certifcateStore;
            this.logger = logger ??  NullLogger<AcmeClient>.Instance;
            
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

            var order = await acmeContext.NewOrder(new[] { "*." + idn.GetAscii(acmeConfig.Host.Substring(2)) });
            var a = await order.Authorizations();
            var authz = a.First();
            var challenge = await authz.Dns();
            var dnsTxt = acmeContext.AccountKey.DnsTxt(challenge.Token);
            logger.LogInformation("Got DNS challenge token {Token}", dnsTxt);

            ///add dns entry
            await this.dnsProvider.PersistChallenge(String.Concat("_acme-challenge.", DnsLookupService.GetNoneWildcardSubdomain(acmeConfig.Host)), dnsTxt);

            if (!(await this.dnsLookupService.Exists(acmeConfig.Host, dnsTxt, this.dnsProvider.MinimumTtl)))
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

            var privateKey = await GetOrCreateKey(acmeConfig.AcmeEnvironment.BaseUri, acmeConfig.Host);
            var cert = await order.Generate(new Certes.CsrInfo
            {
                CountryName = acmeConfig.CsrInfo?.CountryName,
                State = acmeConfig.CsrInfo?.State,
                Locality = acmeConfig.CsrInfo?.Locality,
                Organization = acmeConfig.CsrInfo?.Organization,
                OrganizationUnit = acmeConfig.CsrInfo?.OrganizationUnit                
            }, privateKey);

            var certPem = cert.ToPem();

            var pfxBuilder = cert.ToPfx(privateKey);
            var pfx = pfxBuilder.Build(acmeConfig.Host, acmeConfig.PFXPassword);

            await this.dnsProvider.Cleanup(dnsTxt);

            return new CertificateInstallModel()
            {
                CertificateInfo = new CertificateInfo()
                {
                    Certificate = new X509Certificate2(pfx, acmeConfig.PFXPassword, X509KeyStorageFlags.DefaultKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable),
                    Name = $"{acmeConfig.Host} {DateTime.Now}",
                    Password = acmeConfig.PFXPassword,
                    PfxCertificate = pfx
                },
                Host = acmeConfig.Host
            };
        }

        private async Task<IKey> GetOrCreateKey(Uri acmeDirectory, string host)
        {
            string secretName = $"privatekey{host}--{acmeDirectory.Host}";
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
               // await Task.Delay(10000); //Wait a little before using the new account.
                acme = new AcmeContext(acmeDirectoryUri, acme.AccountKey, new AcmeHttpClient(acmeDirectoryUri, new HttpClient(new MessageLoggingHandler(this.logger))));
            }
            else
            {
                var accountKey = KeyFactory.FromPem(secret);
                acme = new AcmeContext(acmeDirectoryUri, accountKey, new AcmeHttpClient(acmeDirectoryUri, new HttpClient(new MessageLoggingHandler(this.logger))));
            }

            return acme;
        }




    }
}
