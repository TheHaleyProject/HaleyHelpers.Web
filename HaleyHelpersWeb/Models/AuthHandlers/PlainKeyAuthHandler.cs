using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Haley.Enums;

namespace Haley.Models {
    public class PlainKeyAuthHandler : PlainAuthHandlerBase {
        public PlainKeyAuthHandler(IOptionsMonitor<PlainAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder,clock) {
            Options.AuthMode =  PlainAuthMode.APIKey;
        }

        protected override bool GetToken(out string token) {
            token = string.Empty;
            if (!this.Request.Headers.TryGetValue(Options.Name, out var apiKeyHeaderValues)) return false;
            token = apiKeyHeaderValues.FirstOrDefault() ?? string.Empty;
            return true;
        }
    }
}
