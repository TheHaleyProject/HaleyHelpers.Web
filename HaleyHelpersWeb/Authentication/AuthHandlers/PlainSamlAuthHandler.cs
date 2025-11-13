using Haley.Abstractions;
using Haley.Enums;
using Haley.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text.Encodings.Web;
using System.Xml;

namespace Haley.Models {
    public sealed class PlainSamlAuthHandler : PlainAuthHandlerBase<SamlAuthOptions> {
        public PlainSamlAuthHandler(IOptionsMonitor<SamlAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override PlainAuthMode AuthMode { get; set; } = PlainAuthMode.SAML;

        // Accept POSTed SAMLResponse (and optionally query for Redirect binding)
        protected override async Task<IFeedback<string>> GetToken() {
            var fb = new Feedback<string>(false);

            //Remember when Request.Form is accessed it will read the entire body and pareses it as form fields.
            // If the request has multiple form keys with similar names, the post body will be appended.
            if (Request.HasFormContentType) {
                var form = await Request.ReadFormAsync();
                if (form.TryGetValue("SAMLResponse", out var sr)) {
                    return fb.SetStatus(true).SetResult(sr.ToString().Trim());
                }
            }
            return fb;
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
