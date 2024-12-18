using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Haley.Enums;

namespace Haley.Models {
    public class PlainCookieAuthHandler : PlainAuthHandlerBase {
        public PlainCookieAuthHandler(IOptionsMonitor<PlainAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock) {
            Options.AuthMode = PlainAuthMode.Cookie;
        }

        protected override bool GetToken(out string token) {
            token = string.Empty;
            if (!this.Request.Cookies.TryGetValue(Options.Name, out var plainCookie)) return false;
            token = plainCookie ?? string.Empty;
            return true;
        }
    }
}
