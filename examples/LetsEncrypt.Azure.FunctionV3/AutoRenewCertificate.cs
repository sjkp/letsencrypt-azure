using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace LetsEncrypt.Azure.FunctionV3
{
    public static class AutoRenewCertificate
    {
        [FunctionName("AutoRenewCertificate")]
        public static async Task Run([TimerTrigger("%CertRenewSchedule%", RunOnStartup = false)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Renewing certificate at: {DateTime.Now}");

            await Helper.InstallOrRenewCertificate(log);
        }
    }
}
