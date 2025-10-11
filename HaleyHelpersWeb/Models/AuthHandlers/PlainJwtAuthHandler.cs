using Azure.Core;
using Haley.Enums;
using Haley.Models;
using Haley.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Haley.Models {

    public class PlainJwtAuthHandler : PlainAuthHandlerBase<JwtAuthOptions> {

        public PlainJwtAuthHandler(IOptionsMonitor<JwtAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock) {
        }

        protected override PlainAuthMode AuthMode { get; set; } = PlainAuthMode.JWT;

        protected override bool GetToken(out string token) {
            //Options.TokenKey = Bearer
            token = string.Empty;
            if (Request.Headers.TryGetValue("Authorization", out var authHeader) &&
               authHeader.ToString().StartsWith($@"{Options.Key}", StringComparison.OrdinalIgnoreCase)) {
                token = authHeader.ToString().Substring(($@"{Options.Key}".Length)).Trim();
                return true;
            }
            return false;
        }

        protected override Func<HttpContext, string, ILogger, Task<AuthenticateResult>>? Validator => async (c, t, l) => {
            if (string.IsNullOrEmpty(t)) {
                return AuthenticateResult.NoResult();
            }

            var jwtOptions = OptionsMonitor.Get(Scheme.Name);

            try {
                var principal = JWTUtil.ValidateToken(t, jwtOptions.ValidationParams, out var validatedToken);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            } catch (Exception ex) {
                return AuthenticateResult.Fail($"Token validation failed: {ex.Message}");
            }
        };
    }
}