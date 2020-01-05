using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2.Models
{
    public class AzureDnsSettings
    {
        public AzureDnsSettings()
        {
            this.RelativeRecordSetName = "@";
        }

        public AzureDnsSettings(string resourceGroupName, AzureServicePrincipal servicePrincipal, AzureSubscription azureSubscription, string relativeRecordName = "@")
        {
            this.AzureSubscription = azureSubscription;
            this.AzureServicePrincipal = servicePrincipal;
            this.ResourceGroupName = resourceGroupName;
            this.RelativeRecordSetName = resourceGroupName;
        }

        [Required]
        public AzureServicePrincipal AzureServicePrincipal { get; set; }

        [Required]
        public AzureSubscription AzureSubscription { get; set; }

        [Required]
        public string ResourceGroupName { get; set; }

        public string RelativeRecordSetName { get; set; }
    }
}
