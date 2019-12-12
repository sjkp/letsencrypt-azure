﻿using Certes.Acme;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LetsEncrypt.Azure.Core.V2.Models
{
    public class AcmeDnsRequest : IAcmeDnsRequest
    {
        /// <summary>
        /// The email to register with lets encrypt with. Will recieve notifications on expiring certificates.
        /// </summary>
        [Required]
        public string RegistrationEmail { get; set; }

        /// <summary>
        /// The ACME environment, use <see cref="LetsEncryptV2"/> or <see cref="LetsEncryptStagingV2"/> or provide you own ACME compatible endpoint by implementing <see cref="IAcmeEnvironment"/>.
        /// </summary>
        [Required]
        public AcmeEnvironment AcmeEnvironment { get; set; }

        /// <summary>
        /// The host name to request a certificate for e.g. *.example.com
        /// </summary>
        [Required]
        public string Host { get; set; }


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
        /// The ACME environment, use <see cref="LetsEncryptV2"/> or <see cref="LetsEncryptStagingV2"/> or provide you own ACME compatible endpoint by implementing <see cref="AcmeEnvironment"/>.
        /// </summary>
        AcmeEnvironment AcmeEnvironment { get; }

        /// <summary>
        /// The host name to request a certificate for e.g. *.example.com 
        /// </summary>
        string Host { get; }

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

        public AcmeEnvironment()
        {

        }

        public AcmeEnvironment(Uri uri)
        {
            this.BaseUri = uri;
        }

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
        {
            this.name = "staging";
        }
    }

    public class LetsEncryptV2 : AcmeEnvironment
    {
        public LetsEncryptV2() : base(WellKnownServers.LetsEncryptV2)
        {
            this.name = "production";
        }
    }
}
