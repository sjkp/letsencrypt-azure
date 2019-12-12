using System.ComponentModel.DataAnnotations;

namespace LetsEncrypt.Azure.Core.V2.Models
{
    public class AzureWebAppSettings
    {
        public AzureWebAppSettings() { }

        public AzureWebAppSettings(string webappName, string resourceGroup, AzureServicePrincipal servicePrincipal, AzureSubscription azureSubscription, string siteSlotName = null, string servicePlanResourceGroupName = null, bool useIPBasedSSL = false)
        {
            this.WebAppName = webappName;
            this.ResourceGroupName = resourceGroup;
            this.AzureServicePrincipal = servicePrincipal;
            this.AzureSubscription = azureSubscription;
            this.SiteSlotName = siteSlotName;
            this.ServicePlanResourceGroupName = servicePlanResourceGroupName;
            this.UseIPBasedSSL = useIPBasedSSL;
        }

        [Required]
        public string WebAppName { get; set; }

        [Required]
        public string ResourceGroupName { get; set; }

        public string ServicePlanResourceGroupName { get; set; }

        public string SiteSlotName { get; set; }

        public bool UseIPBasedSSL { get; set; }

        [Required]
        public AzureServicePrincipal AzureServicePrincipal { get; set; }

        [Required]
        public AzureSubscription AzureSubscription { get; set; }
    }
}
