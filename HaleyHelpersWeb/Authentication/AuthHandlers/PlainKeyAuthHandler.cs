using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Haley.Enums;

namespace Haley.Models {
    public class PlainKeyAuthHandler : PlainAuthHandlerBase<PlainAuthOptions> {
        public PlainKeyAuthHandler(IOptionsMonitor<PlainAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder,clock) {
        }
        protected override PlainAuthMode AuthMode { get; set; } = PlainAuthMode.APIKey;
        protected override bool GetToken(out string token) {
            token = string.Empty;
            if (!this.Request.Headers.TryGetValue(Options.Key, out var apiKeyHeaderValues)) return false;
            token = apiKeyHeaderValues.FirstOrDefault() ?? string.Empty;
            return true;
        }
    }
}
