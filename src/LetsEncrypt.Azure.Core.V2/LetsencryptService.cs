﻿using LetsEncrypt.Azure.Core.V2.CertificateConsumers;
using LetsEncrypt.Azure.Core.V2.CertificateStores;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2
{
    public class LetsencryptService
    {
        private readonly AcmeClient acmeClient;
        private readonly ICertificateStore certificateStore;
        private readonly ICertificateConsumer certificateConsumer;
        private readonly ILogger<LetsencryptService> logger;

        public LetsencryptService(AcmeClient acmeClient, ICertificateStore certificateStore, ICertificateConsumer certificateConsumer, ILogger<LetsencryptService> logger = null)
        {
            this.acmeClient = acmeClient;
            this.certificateStore = certificateStore;
            this.certificateConsumer = certificateConsumer;
            this.logger = logger ?? NullLogger<LetsencryptService>.Instance;
        }

        public async Task Run(IAcmeDnsRequest acmeDnsRequest, int renewXNumberOfDaysBeforeExpiration)
        {
            try
            {
                CertificateInstallModel model = null;

                string hostsPlusSeparated = AcmeClient.GetHostsPlusSeparated(acmeDnsRequest.Hosts);
                var certname = $"{hostsPlusSeparated}-{acmeDnsRequest.AcmeEnvironment.Name}";
                var cert = await certificateStore.GetCertificate(certname, acmeDnsRequest.PFXPassword);
                if (cert == null || cert.Certificate.NotAfter < DateTime.UtcNow.AddDays(renewXNumberOfDaysBeforeExpiration)) //Cert doesnt exist or expires in less than renewXNumberOfDaysBeforeExpiration days, lets renew.
                {
                    logger.LogInformation("Certificate store didn't contain certificate or certificate was expired starting renewing");
                    model = await acmeClient.RequestDnsChallengeCertificate(acmeDnsRequest);
                    model.CertificateInfo.Name = certname;
                    await certificateStore.SaveCertificate(model.CertificateInfo);
                }
                else
                {
                    logger.LogInformation("Certificate expires in more than {renewXNumberOfDaysBeforeExpiration} days, reusing certificate from certificate store", renewXNumberOfDaysBeforeExpiration);
                    model = new CertificateInstallModel()
                    {
                        CertificateInfo = cert,
                        Hosts = acmeDnsRequest.Hosts
                    };
                }
                await certificateConsumer.Install(model);

                logger.LogInformation("Removing expired certificates");
                var expired = await certificateConsumer.CleanUp();
                logger.LogInformation("The following certificates was removed {Thumbprints}", string.Join(", ", expired.ToArray()));

            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed");
                throw;
            }
        }
    }
}
