using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace LetsEncrypt.Azure.FunctionV2
{
    public static class AutoRenewCertificate
    {
        [FunctionName("AutoRenewCertificate")]
        public static async Task Run([TimerTrigger("24:00:00", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Renewing certificate at: {DateTime.Now}");

            await Helper.InstallOrRenewCertificate(log);
        }
    }
}
