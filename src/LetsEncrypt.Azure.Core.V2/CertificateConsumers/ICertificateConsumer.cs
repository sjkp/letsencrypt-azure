using LetsEncrypt.Azure.Core.V2.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.Azure.Core.V2.CertificateConsumers
{
    public interface ICertificateConsumer
    {
        /// <summary>
        /// Installs/assigns the new certificate.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task Install(ICertificateInstallModel model);

        /// <summary>
        /// Remove any expired certificates
        /// </summary>
        /// <returns>List of thumbprint for certificates removed</returns>
        Task<List<string>> CleanUp();
    }
}
