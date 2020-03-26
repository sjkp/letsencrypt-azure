namespace LetsEncrypt.Azure.Core.V2.Models
{
    public class AzureWebAppSettings
    {
        public AzureWebAppSettings()
        {

        }
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
        public string WebAppName { get; set; }
        public string ResourceGroupName { get; set; }

        public string ServicePlanResourceGroupName { get; set; }

        /// <summary>
        /// Returns service plan resource group name unless empty then returns resource group name.
        /// </summary>
        public string ServicePlanOrResourceGroupName
        {
            get
            {
                return string.IsNullOrEmpty(ServicePlanResourceGroupName) ? ResourceGroupName : ServicePlanResourceGroupName;
            }
        }

        public string SiteSlotName { get; set; }

        public bool UseIPBasedSSL { get; set; }

        public AzureServicePrincipal AzureServicePrincipal { get; set; }

        public AzureSubscription AzureSubscription { get; set; }
    }
}
