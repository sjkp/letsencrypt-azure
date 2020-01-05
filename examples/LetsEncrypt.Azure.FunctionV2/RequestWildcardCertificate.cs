using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using LetsEncrypt.Azure.Core.V2;
using LetsEncrypt.Azure.Core.V2.DnsProviders;
using LetsEncrypt.Azure.Core.V2.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using System.Web.Http;

namespace LetsEncrypt.Azure.FunctionV2
{
    public static class RequestWildcardCertificate
    {
        [FunctionName("RequestWildcardCertificate")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                await Helper.InstallOrRenewCertificate(log);

                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
                return new ExceptionResult(ex, true);
            }
        }
    }
}
