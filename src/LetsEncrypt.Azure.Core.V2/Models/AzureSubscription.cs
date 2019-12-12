using System.ComponentModel.DataAnnotations;

namespace LetsEncrypt.Azure.Core.V2.Models
{
    public class AzureSubscription
    {
        [Required]
        public string Tenant { get; set; }
        [Required]
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Should be AzureGlobalCloud, AzureChinaCloud, AzureUSGovernment or AzureGermanCloud
        /// </summary>
        [Required]
        public string AzureRegion { get; set; }
    }
}