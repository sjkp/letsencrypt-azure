using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using System;

namespace LetsEncrypt.Azure.Core.V2
{
    public class AzureHelper
    {
        public static AzureCredentials GetAzureCredentials(AzureServicePrincipal servicePrincipal, AzureSubscription azureSubscription)
        {
            if (servicePrincipal == null)
            {
                throw new ArgumentNullException(nameof(servicePrincipal));
            }

            if (azureSubscription == null)
            {
                throw new ArgumentNullException(nameof(azureSubscription));
            }

            if (servicePrincipal.UseManagendIdentity)
            {
                return new AzureCredentials(new MSILoginInformation(MSIResourceType.AppService), Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.FromName(azureSubscription.AzureRegion));
            }

            return new AzureCredentials(servicePrincipal.ServicePrincipalLoginInformation,
               azureSubscription.Tenant, Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.FromName(azureSubscription.AzureRegion));
        }

        public static RestClient GetRestClient(AzureServicePrincipal servicePrincipal, AzureSubscription azureSubscription)
        {
            var credentials = GetAzureCredentials(servicePrincipal, azureSubscription);
            return RestClient
                .Configure()
                .WithEnvironment(Microsoft.Azure.Management.ResourceManager.Fluent.AzureEnvironment.FromName(azureSubscription.AzureRegion))
                .WithCredentials(credentials)
                .Build();
        }
    }
}
