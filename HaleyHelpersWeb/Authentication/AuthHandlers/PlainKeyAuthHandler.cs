using Haley.Abstractions;
using Haley.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Haley.Models {
    public class PlainKeyAuthHandler : PlainAuthHandlerBase<PlainAuthOptions> {
        public PlainKeyAuthHandler(IOptionsMonitor<PlainAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder,clock) {
        }
        protected override PlainAuthMode AuthMode { get; set; } = PlainAuthMode.APIKey;

        protected override async Task<IFeedback<string>> GetToken() {
            var fb = new Feedback<string>().SetStatus(false);
            if (!this.Request.Headers.TryGetValue(Options.Key, out var apiKeyHeaderValues) || string.IsNullOrWhiteSpace(apiKeyHeaderValues.FirstOrDefault())) return fb;
            return fb.SetStatus(true).SetResult(apiKeyHeaderValues.FirstOrDefault()!);
        }
    }
}
