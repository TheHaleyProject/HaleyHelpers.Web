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
                if (string.IsNullOrWhiteSpace(base64Xml)) return AuthenticateResult.NoResult();
                return SamlHelpers.ValidateAzurePayload(Request, base64Xml, log, Scheme.Name, OptionsMonitor.Get(Scheme.Name));
            } catch (Exception ex) {
                log?.LogError(ex, "SAML validation failed");
                return AuthenticateResult.Fail("SAML validation failed");
            }
        };
    }
}
