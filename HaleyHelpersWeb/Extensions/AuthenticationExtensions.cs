using Haley.Abstractions;
using Haley.Models;
using Microsoft.AspNetCore.Authentication;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace Haley.Utils {
    public static class AuthenticationExtensions {
        public static AuthenticationBuilder AddAzureSamlScheme(this AuthenticationBuilder builder, IConfiguration configuration, string schemeName = BaseSchemeNames.AzureSAML, Action<SamlAuthOptions>? configure = null) {
            // Pull section: Saml:Azure
            var section = configuration.GetSection("Saml:Azure");
            if (!section.Exists()) throw new InvalidOperationException("Missing 'Saml:Azure' section in configuration.");

            var spEntityId = section["SpEntityId"] ?? throw new InvalidOperationException("Saml:Azure:SpEntityId is required");
            var acsUrl = section["AcsUrl"] ?? throw new InvalidOperationException("Saml:Azure:AcsUrl is required");
            var idpMetadataUrl = section["IdpMetadataUrl"] ?? string.Empty;

            // ----- Certificate handling -----
            var certPath = section["CertPath"];
            var certBase64 = section["CertBase64"];

            X509Certificate2? cert = null;

            if (!string.IsNullOrWhiteSpace(certBase64)) {
                try {
                    cert = new X509Certificate2(Convert.FromBase64String(certBase64));
                } catch (Exception ex) {
                    throw new InvalidOperationException("Failed to load SAML certificate from CertBase64.", ex);
                }
            } else if (!string.IsNullOrWhiteSpace(certPath)) {
                if (!File.Exists(certPath)) throw new FileNotFoundException($"SAML certificate file not found at path: {certPath}");
                cert = new X509Certificate2(certPath);
            } else {
                throw new InvalidOperationException("Either 'CertBase64' or 'CertPath' must be provided in SAML config.");
            }

            // Register the scheme with default options
            builder.AddScheme<SamlAuthOptions, PlainSamlAuthHandler>(schemeName, options => {
                options.SpEntityId = spEntityId;
                options.AcsUrl = acsUrl;
                options.IdpMetadataUrl = idpMetadataUrl;
                options.SigningCert = cert;
                options.Audience = options.SpEntityId;

                configure?.Invoke(options); // Allow overrides
            });

            return builder;
        }
    }
}
