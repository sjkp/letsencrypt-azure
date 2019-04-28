using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LetsEncrypt.Azure.Core.V2.Models;

namespace LetsEncrypt.Azure.Core.V2.CertificateConsumers
{
    /// <summary>
    /// Certificate consumer that does do anything.
    /// </summary>
    public class NullCertificateConsumer : ICertificateConsumer
    {
        public Task<List<string>> CleanUp()
        {
            return Task.FromResult(new List<string>());
        }

        public Task Install(ICertificateInstallModel model)
        {
            return Task.CompletedTask;
        }
    }
}
