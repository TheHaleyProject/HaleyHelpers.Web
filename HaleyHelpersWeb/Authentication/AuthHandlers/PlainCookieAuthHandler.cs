using Haley.Abstractions;
using Haley.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Haley.Models {
    public class PlainCookieAuthHandler : PlainAuthHandlerBase<PlainAuthOptions> {
        public PlainCookieAuthHandler(IOptionsMonitor<PlainAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock) {
        }

        protected override PlainAuthMode AuthMode { get; set; } = PlainAuthMode.Cookie;

        protected override async Task<IFeedback<string>> GetToken() {
            var fb = new Feedback<string>().SetStatus(false);
            if (!Request.Cookies.TryGetValue(Options.Key, out var plainCookie) || string.IsNullOrWhiteSpace(plainCookie)) return fb;
            return fb.SetStatus(true).SetResult(plainCookie);
        }
    }
}
