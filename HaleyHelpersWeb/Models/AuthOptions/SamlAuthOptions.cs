using Haley.Enums;

using System.Security.Cryptography.X509Certificates;
namespace Haley.Models {
    public class SamlAuthOptions : PlainAuthOptions {
        // IdP (Entra) metadata URL or static cert 
        public string IdpMetadataUrl { get; set; } = default!;
        public X509Certificate2? SigningCert { get; set; }   // if you load cert from IT’s attachment

        // SP (your app) settings
        public string SpEntityId { get; set; } = default!;   // e.g., https://yourapp.com/saml
        public string AcsUrl { get; set; } = default!;       // e.g., https://yourapp.com/auth/saml/acs
        public string? Audience { get; set; }                // default to SpEntityId if null
        public TimeSpan AllowedClockSkew { get; set; } = TimeSpan.FromMinutes(3);
        public SamlAuthOptions() { base.Key = "SAMLResponse"; }
    }
}
