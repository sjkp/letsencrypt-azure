using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2.Models
{
    /// <summary>
    /// Result of the certificate installation.
    /// </summary>
    public class CertificateInstallModel : ICertificateInstallModel
    {
        /// <summary>
        /// Certificate info.
        /// </summary>
        public CertificateInfo CertificateInfo { get; set; }

        /// <summary>
        /// The primary host name.
        /// </summary>
        public ImmutableArray<string> Hosts { get; set; }
    }

    public interface ICertificateInstallModel
    {
        CertificateInfo CertificateInfo { get; set; }

        ImmutableArray<string> Hosts { get; set; }
    }
}
