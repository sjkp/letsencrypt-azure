using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.AppService.Fluent.Models;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LetsEncrypt.Azure.Core.V2.CertificateConsumers;

namespace LetsEncrypt.Azure.Core.V2
{
    public class AzureWebAppService : ICertificateConsumer
    {
        private readonly AzureWebAppSettings[] settings;
        private readonly ILogger<AzureWebAppService> logger;

        public AzureWebAppService(AzureWebAppSettings[] settings, ILogger<AzureWebAppService> logger = null)
        {
            this.settings = settings;
            this.logger = logger ?? NullLogger<AzureWebAppService>.Instance;
        }

        public async Task Install(ICertificateInstallModel model)
        {
            string hostsComaSeparated = string.Join(",", model.Hosts);
            logger.LogInformation("Starting installation of certificate {Thumbprint} for {Host}", model.CertificateInfo.Certificate.Thumbprint, hostsComaSeparated);
            var cert = model.CertificateInfo;
            foreach (var setting in this.settings)
            {
                logger.LogInformation("Installing certificate for web app {WebApp}", setting.WebAppName);
                try
                {
                    IAppServiceManager appServiceManager = GetAppServiceManager(setting);
                    var s = appServiceManager.WebApps.GetByResourceGroup(setting.ResourceGroupName, setting.WebAppName);
                    IWebAppBase siteOrSlot = s;
                    if (!string.IsNullOrEmpty(setting.SiteSlotName))
                    {
                        var slot = s.DeploymentSlots.GetByName(setting.SiteSlotName);
                        siteOrSlot = slot;
                    }

                    var existingCerts = await appServiceManager.AppServiceCertificates.ListByResourceGroupAsync(setting.ServicePlanResourceGroupName ?? setting.ResourceGroupName);
                    if (existingCerts.All(_ => _.Thumbprint != cert.Certificate.Thumbprint))
                    {
                        await appServiceManager
                            .AppServiceCertificates
                            .Define($"{hostsComaSeparated}-{cert.Certificate.Thumbprint}")
                            .WithRegion(s.RegionName)
                            .WithExistingResourceGroup(setting.ServicePlanResourceGroupName ?? setting.ResourceGroupName)
                            .WithPfxByteArray(model.CertificateInfo.PfxCertificate)
                            .WithPfxPassword(model.CertificateInfo.Password)
                            .CreateAsync();
                    }

                    var sslStates = siteOrSlot.HostNameSslStates;
                    var domainSslMappings = new List<KeyValuePair<string, HostNameSslState>>(
                        sslStates.Where(_ =>
                            model.Hosts.Any(h => _.Key == h
                                              || _.Key.Contains($".{h}"))));

                    if (domainSslMappings.Any())
                    {
                        foreach (var domainMapping in domainSslMappings)
                        {

                            string hostName = domainMapping.Value.Name;
                            if (domainMapping.Value.Thumbprint == cert.Certificate.Thumbprint)
                                continue;
                            logger.LogInformation("Binding certificate {Thumbprint} to {Host}", model.CertificateInfo.Certificate.Thumbprint, hostName);
                            var binding = new HostNameBindingInner()
                            {
                                SslState = setting.UseIPBasedSSL ? SslState.IpBasedEnabled : SslState.SniEnabled,
                                Thumbprint = model.CertificateInfo.Certificate.Thumbprint
                            };
                            if (!string.IsNullOrEmpty(setting.SiteSlotName))
                            {
                                await appServiceManager.Inner.WebApps.CreateOrUpdateHostNameBindingSlotAsync(setting.ServicePlanResourceGroupName ?? setting.ResourceGroupName, setting.WebAppName, hostName, binding, setting.SiteSlotName);
                            }
                            else
                            {
                                await appServiceManager.Inner.WebApps.CreateOrUpdateHostNameBindingAsync(setting.ServicePlanResourceGroupName ?? setting.ResourceGroupName, setting.WebAppName, hostName, binding);
                            }
                        }
                    }
                }
                // TODO: Catch errors one by one
                catch (Exception e)
                {
                    logger.LogCritical(e, "Unable to install certificate for '{WebApp}'", setting.WebAppName);
                    throw;
                }
            }
        }

        private static IAppServiceManager GetAppServiceManager(AzureWebAppSettings settings)
        {
            var restClient = AzureHelper.GetRestClient(settings.AzureServicePrincipal, settings.AzureSubscription);
            return new AppServiceManager(restClient, settings.AzureSubscription.SubscriptionId, settings.AzureSubscription.Tenant);
        }

        public async Task<List<string>> CleanUp()
        {
            return await this.CleanUp(0);
        }
        public async Task<List<string>> CleanUp(int removeXNumberOfDaysBeforeExpiration = 0)
        {
            var removedCerts = new List<string>();
            foreach (var setting in this.settings)
            {
                var appServiceManager = GetAppServiceManager(setting);
                var certs = await appServiceManager.AppServiceCertificates.ListByResourceGroupAsync(setting.ServicePlanResourceGroupName ?? setting.ResourceGroupName);

                var tobeRemoved = certs.Where(s => s.ExpirationDate < DateTime.UtcNow.AddDays(removeXNumberOfDaysBeforeExpiration) && (s.Issuer.Contains("Let's Encrypt") || s.Issuer.Contains("Fake LE"))).ToList();

                tobeRemoved.ForEach(async s => await RemoveCertificate(appServiceManager, s, setting));

                removedCerts.AddRange(tobeRemoved.Select(s => s.Thumbprint).ToList());
            }
            return removedCerts;
        }

        private async Task RemoveCertificate(IAppServiceManager webSiteClient, IAppServiceCertificate s, AzureWebAppSettings setting)
        {
            await webSiteClient.AppServiceCertificates.DeleteByResourceGroupAsync(setting.ServicePlanResourceGroupName ?? setting.ResourceGroupName, s.Name);
        }
    }
}

