using Haley.Abstractions;
using Haley.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Haley.Utils {
    public static class WebAuthUtils {
        public static AuthenticationBuilder AddAzureSamlScheme(this AuthenticationBuilder builder, string schemeName = BaseSchemeNames.FORM_TOKEN_AZURE_SAML, Action<SamlAuthOptions>? configure = null) {
            var configuration = ResourceUtils.GenerateConfigurationRoot();
            if (configuration == null) throw new InvalidOperationException("Failed to load configuration.");
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
            builder.AddScheme<SamlAuthOptions, PlainAzureSamlAuthHandler>(schemeName, options => {
                options.SpEntityId = spEntityId;
                options.AcsUrl = acsUrl;
                options.IdpMetadataUrl = idpMetadataUrl;
                options.SigningCert = cert;
                options.Audience = options.SpEntityId;

                configure?.Invoke(options); // Allow overrides
            });

            return builder;
        }
        public static string GetClientIP(this HttpContext context) {
            try {
                return context.Connection.RemoteIpAddress?.ToString();
            } catch (Exception) {
                return string.Empty;
            }
        }
        public static string GetEncryptedIPCookie(this HttpContext context, string encryptKey, string encryptSalt = null, Dictionary<string, string> payload = null) {
            var ip = context.GetClientIP();
            if (string.IsNullOrWhiteSpace(ip)) throw new ArgumentException("Unable to verify the ip address of the request.");
            if (string.IsNullOrWhiteSpace(encryptKey)) throw new ArgumentException("Encryption Key cannot be empty or null");
            if (string.IsNullOrWhiteSpace(encryptSalt)) encryptSalt = encryptKey; //assign the key as the salt as well

            var cookieDic = new Dictionary<string, string> {
                ["ip"] = ip
            };
            if (payload != null) {
                foreach (var item in payload) {
                    if (!cookieDic.ContainsKey(item.Key)) {
                        cookieDic.Add(item.Key, item.Value);
                    }
                }
            }

            return EncryptionUtils.Encrypt(cookieDic.ToJson(), encryptKey, encryptSalt).value;
        }
        public static void AppendEncryptIPCookie(this HttpContext context, string cookieName, CookieOptions options, string encryptKey, string encryptSalt = null, Dictionary<string, string> payload = null) {
            try {
                if (context == null) throw new ArgumentNullException("HttpContext");
                if (string.IsNullOrWhiteSpace(cookieName)) throw new ArgumentNullException("CookieName is required for creation");
                var cookie = GetEncryptedIPCookie(context, encryptKey, encryptSalt, payload);
                context.Response.Cookies.Append(cookieName, cookie, options);
            } catch (Exception) {
                context.Response.Body = new MemoryStream(Encoding.UTF8.GetBytes("Error while trying to add cookie"));
            }
        }
        public static Dictionary<string, string> DecryptIPCookie(this string cookie, string encryptKey, string encryptSalt = null) {
            if (string.IsNullOrWhiteSpace(cookie)) throw new ArgumentNullException("Cookie value cannot be null for decryption.");
            if (string.IsNullOrWhiteSpace(encryptKey)) throw new ArgumentException("Encryption Key cannot be empty or null");
            if (string.IsNullOrWhiteSpace(encryptSalt)) encryptSalt = encryptKey; //assign the key as the salt as well
            var decrypted = EncryptionUtils.Decrypt(cookie, encryptKey, encryptSalt);
            return decrypted.FromJson<Dictionary<string, string>>();
        }

        public static void AddAzureSamlPolicy(this AuthorizationOptions options, string policyName, string schemeName = BaseSchemeNames.FORM_TOKEN_AZURE_SAML) => options.WithSchemes(schemeName).ForPolicy(policyName).CreateAuthPolicy();
    }
}
