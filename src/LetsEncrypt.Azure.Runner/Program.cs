using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using LetsEncrypt.Azure.Core.V2;
using Microsoft.Extensions.Configuration;
using LetsEncrypt.Azure.Core.V2.Models;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using LetsEncrypt.Azure.Core;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core.V2.CertificateStores;
using Microsoft.Azure.KeyVault;
using Microsoft.Rest;

namespace LetsEncrypt.Azure.Runner
{
    class Program
    {
        static IConfiguration Configuration;
        async static Task Main(string[] args)
        {
            Configuration = new ConfigurationBuilder()
                  .AddJsonFile("settings.json", true)
                  .AddEnvironmentVariables()
                  .Build();

            IServiceCollection serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(c =>
            {
                c.AddConsole();
                //c.AddDebug();
            })
            .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information);

            var azureAppSettings = new AzureWebAppSettings[] { };
            
            if (Configuration.GetSection("AzureAppService").Exists())
            {
                azureAppSettings = new[] { Configuration.GetSection("AzureAppService").Get<AzureWebAppSettings>() };
            }
            if (Configuration.GetSection("AzureAppServices").Exists())
            {
                azureAppSettings = Configuration.GetSection("AzureAppServices").Get<AzureWebAppSettings[]>();
            }

            if (azureAppSettings.Length == 0)
            {
                serviceCollection.AddNullCertificateConsumer();
            }
            else
            {
                serviceCollection.AddAzureAppService(azureAppSettings);
            }

            if (!string.IsNullOrEmpty(Configuration.GetSection("CertificateStore").Get<BlobCertificateStoreAppSettings>().ConnectionString))
            {
                var blobSettings = Configuration.GetSection("CertificateStore").Get<BlobCertificateStoreAppSettings>();
                serviceCollection.AddAzureBlobStorageCertificateStore(blobSettings.ConnectionString);
            }
            else if (Configuration.GetSection("CertificateStore").Get<KeyVaultCertificateStoreAppSettings>().BaseUrl != null)
            {
                var keyVaultSettings = Configuration.GetSection("CertificateStore").Get<KeyVaultCertificateStoreAppSettings>();
                serviceCollection.AddTransient<IKeyVaultClient>((service) =>
                {
                    return new KeyVaultClient(AzureHelper.GetAzureCredentials(keyVaultSettings.AzureServicePrincipal, keyVaultSettings.AzureSubscription), new MessageLoggingHandler(service.GetService<ILogger>()));
                });
                serviceCollection.AddKeyVaultCertificateStore(keyVaultSettings.BaseUrl);
            }
            else
            {
                //Nothing default a null certificate store will be added.
            }
                        
            

            if (Configuration.GetSection("DnsSettings").Get<GoDaddyDnsProvider.GoDaddyDnsSettings>().ShopperId != null)
            {
                serviceCollection.AddAcmeClient<GoDaddyDnsProvider>(Configuration.GetSection("DnsSettings").Get<GoDaddyDnsProvider.GoDaddyDnsSettings>());
            } else if (Configuration.GetSection("DnsSettings").Get<UnoEuroDnsSettings>().AccountName != null)
            {
                serviceCollection.AddAcmeClient<UnoEuroDnsProvider>(Configuration.GetSection("DnsSettings").Get<UnoEuroDnsSettings>());
            } else if (Configuration.GetSection("DnsSettings").Get<AzureDnsSettings>().ResourceGroupName != null)
            {
                serviceCollection.AddAcmeClient<AzureDnsProvider>(Configuration.GetSection("DnsSettings").Get<AzureDnsSettings>());
            }

            serviceCollection.AddTransient<LetsencryptService>();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var dnsRequest = Configuration.GetSection("AcmeDnsRequest").Get<AcmeDnsRequest>();

            var app = serviceProvider.GetService<LetsencryptService>();
            await app.Run(dnsRequest, Configuration.GetValue<int?>("RenewXNumberOfDaysBeforeExpiration") ?? 22);
        }
    }
}
