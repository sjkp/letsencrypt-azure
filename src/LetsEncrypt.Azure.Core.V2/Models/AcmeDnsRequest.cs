using Certes.Acme;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2.Models
{
    public class DomainComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            var xParts = x.Split('.').Reverse().ToImmutableArray();
            var yParts = y.Split('.').Reverse().ToImmutableArray();
            var result = xParts.Zip(yParts, (xPart, yPart) => string.Compare(xPart, yPart)).FirstOrDefault(r => r != 0);
            if (result == 0)
                result = xParts.Length - yParts.Length;
            return result;
        }
    }

    public class AcmeDnsRequest : IAcmeDnsRequest
    {
        private readonly static IComparer<string> domainComparer = new DomainComparer();

        /// <summary>
        /// The email to register with lets encrypt with. Will recieve notifications on expiring certificates.
        /// </summary>
        [Required]
        public string RegistrationEmail { get; set; }

        /// <summary>
        /// The ACME environment, use <see cref="LetsEncryptV2"/> or <see cref="LetsEncryptStagingV2"/> or provide you
        /// own ACME compatible endpoint by implementing <see cref="IAcmeEnvironment"/>.
        /// </summary>
        [Required]
        public AcmeEnvironment AcmeEnvironment { get; set; }

        private string hosts;
        private ImmutableArray<string> hostsList = ImmutableArray<string>.Empty;
        private ImmutableArray<string> domainsList = ImmutableArray<string>.Empty;

        /// <summary>
        /// The host names to request a certificate for delimited by coma e.g. *.example1.com,*.example2.com
        /// </summary>
        [Required]
        public string Hosts
        {
            get { return hosts; }
            set
            {
                string RemoveWildcard(string host) => host.StartsWith("*.") ? host.Substring(2) : host;

                this.hosts = value;
                string[] hosts = this.hosts.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                hostsList = hosts.OrderBy(h => h, domainComparer).Distinct().ToImmutableArray();
                domainsList = hostsList
                    .Select(RemoveWildcard)
                    .Distinct()
                    .ToImmutableArray();
            }
        }

        ImmutableArray<string> IAcmeDnsRequest.Hosts => hostsList;

        ImmutableArray<string> IAcmeDnsRequest.Domains => domainsList;

        [Required]
        public string PFXPassword { get; set; }

        [Required]
        public CsrInfo CsrInfo { get; set; }
    }

    public interface IAcmeDnsRequest
    {
        /// <summary>
        /// The email to register with lets encrypt with. Will recieve notifications on expiring certificates.
        /// </summary>
        string RegistrationEmail { get; }

        /// <summary>
        /// The ACME environment, use <see cref="LetsEncryptV2"/> or <see cref="LetsEncryptStagingV2"/> or provide you own ACME
        /// compatible endpoint by implementing <see cref="AcmeEnvironment"/>.
        /// </summary>
        AcmeEnvironment AcmeEnvironment { get; }

        /// <summary>
        /// A list of host names to request a certificate for without a wildcard symbol e.g. example.com
        /// </summary>
        ImmutableArray<string> Hosts { get; }

        /// <summary>
        /// A list of domain zones for which certificate will be requested
        /// </summary>
        ImmutableArray<string> Domains { get; }

        string PFXPassword { get; }

        CsrInfo CsrInfo { get; }
    }

    public class CsrInfo
    {
        public string CountryName { get; set; }

        public string State { get; set; }

        public string Locality { get; set; }

        public string Organization { get; set; }

        public string OrganizationUnit { get; set; }

        public string CommonName { get; set; }
    }

    public class AcmeEnvironment
    {
        public Uri BaseUri { get; set; }

        public AcmeEnvironment() { }

        public AcmeEnvironment(Uri uri) { this.BaseUri = uri; }

        protected string name;

        public string Name
        {
            get => name;
            set
            {
                if ("production".Equals(value, StringComparison.InvariantCultureIgnoreCase))
                {
                    BaseUri = WellKnownServers.LetsEncryptV2;
                }
                else
                {
                    BaseUri = WellKnownServers.LetsEncryptStagingV2;
                }
                name = value;
            }
        }
    }


    public class LetsEncryptStagingV2 : AcmeEnvironment
    {
        public LetsEncryptStagingV2() : base(WellKnownServers.LetsEncryptStagingV2)
         => this.name = "staging";
    }

    public class LetsEncryptV2 : AcmeEnvironment
    {
        public LetsEncryptV2() : base(WellKnownServers.LetsEncryptV2)
         => this.name = "production";
    }
}
