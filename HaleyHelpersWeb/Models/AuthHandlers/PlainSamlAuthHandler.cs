using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Haley.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using Haley.Utils;

namespace Haley.Models {
    public sealed class PlainSamlAuthHandler : PlainAuthHandlerBase<SamlAuthOptions> {
        public PlainSamlAuthHandler(IOptionsMonitor<SamlAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override PlainAuthMode AuthMode { get; set; } = PlainAuthMode.SAML;

        // Accept POSTed SAMLResponse (and optionally query for Redirect binding)
        protected override bool GetToken(out string token) {
            token = string.Empty;
            if (Request.HasFormContentType && Request.Form.TryGetValue("SAMLResponse", out var sr)) {
                token = sr.ToString();
                return true;
            }
            if (Request.Query.TryGetValue("SAMLResponse", out var qr)) {
                token = qr.ToString();
                return true;
            }
            return false;
        }

        // Use handler-level validator (so Options.Validator can stay null)
        protected override Func<HttpContext, string, ILogger, Task<AuthenticateResult>>? Validator => async (ctx, base64Xml, log) => {
            try {
                if (string.IsNullOrWhiteSpace(base64Xml))
                    return AuthenticateResult.NoResult();

                var o = OptionsMonitor.Get(Scheme.Name);
                var audience = string.IsNullOrWhiteSpace(o.Audience) ? o.SpEntityId : o.Audience;

                // 1) Decode XML
                var xmlBytes = Convert.FromBase64String(base64Xml);
                var xml = System.Text.Encoding.UTF8.GetString(xmlBytes);

                // 2) Load XML
                var doc = new XmlDocument { PreserveWhitespace = true };
                doc.LoadXml(xml);

                // 3) Get signing cert (from options or metadata loader you register via DI)
                //Idea behind the samlmetadacache is to avoid fetching metadata on every request. Its like, we download the certificate from the metadata once and cache it for later use. Sometimes, the server will rotate the certificate, so we need to have a way to refresh it periodically.

                var cert = o.SigningCert; //?? await SamlMetadataCache.GetSigningCertAsync(o.IdpMetadataUrl, ctx.RequestServices, log); 

                // 4) Validate signature on Response or Assertion
                if (!ValidateSignature(doc, cert))
                    return AuthenticateResult.Fail("Invalid SAML signature");

                // 5) Validate Conditions (NotBefore/NotOnOrAfter) and Audience
                if (!ValidateConditionsAndAudience(doc, audience, o.AllowedClockSkew))
                    return AuthenticateResult.Fail("SAML conditions/audience check failed");

                // 6) Extract claims (NameID + attributes you care about)
                var claims = SamlHelpers.ExtractClaims(doc, o.SpEntityId);

                // 7) Success -> ticket
                var identity = new System.Security.Claims.ClaimsIdentity(claims, Scheme.Name);
                var principal = new System.Security.Claims.ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            } catch (Exception ex) {
                log?.LogError(ex, "SAML validation failed");
                return AuthenticateResult.Fail("SAML validation failed");
            }
        };

        static bool ValidateSignature(XmlDocument doc, X509Certificate2 cert) {
            // Try Response-level signature first
            var sigEl = (XmlElement?)doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl).Cast<XmlElement?>().FirstOrDefault();
            if (sigEl == null) return false;

            var parent = sigEl.ParentNode as XmlElement;   // Response or Assertion
            var sx = new SignedXml(parent);
            sx.LoadXml(sigEl);
            return sx.CheckSignature(cert, true);
        }

        static bool ValidateConditionsAndAudience(XmlDocument doc, string audience, TimeSpan skew) {
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("saml", SamlHelpers.NsSaml);

            var conditions = doc.SelectSingleNode("//saml:Assertion/saml:Conditions", ns) as XmlElement;
            if (conditions != null) {
                DateTime? nb = TryParse(conditions.GetAttribute("NotBefore"));
                DateTime? na = TryParse(conditions.GetAttribute("NotOnOrAfter"));
                var now = DateTime.UtcNow;
                if (nb.HasValue && now + skew < nb.Value) return false;
                if (na.HasValue && now - skew >= na.Value) return false;
            }

            var aud = doc.SelectSingleNode("//saml:Audience", ns)?.InnerText?.Trim();
            return string.IsNullOrWhiteSpace(aud) || string.Equals(aud, audience, StringComparison.OrdinalIgnoreCase);

            static DateTime? TryParse(string v) => string.IsNullOrWhiteSpace(v) ? null : DateTime.Parse(v, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        }
    }
}
