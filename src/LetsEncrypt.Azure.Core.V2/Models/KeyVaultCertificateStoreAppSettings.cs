using System;
using System.Collections.Generic;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2.Models
{
    public class KeyVaultCertificateStoreAppSettings
    {
        public AzureServicePrincipal AzureServicePrincipal { get; set; }

        public AzureSubscription AzureSubscription { get; set; }

        public string BaseUrl { get; set; }
    }
}
