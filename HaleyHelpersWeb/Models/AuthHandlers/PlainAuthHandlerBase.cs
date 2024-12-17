using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Haley.Models {
    public abstract class PlainAuthHandlerBase : AuthenticationHandler<PlainAuthOptions> {
        public PlainAuthHandlerBase(IOptionsMonitor<PlainAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock) {
        }

        protected abstract bool GetToken(out string token);

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() {

            if (Options.Validator == null) return Task.FromResult(AuthenticateResult.Fail("Auth Validator is missing"));

            if (!GetToken(out var token)) {
                return Task.FromResult(AuthenticateResult.Fail($@"Unable to find a {Options.AuthMode.ToString()} with name {Options.Name}"));
            }

            if (string.IsNullOrWhiteSpace(token)) return Task.FromResult(AuthenticateResult.Fail($@"{Options.AuthMode.ToString()} token value cannot be null or empty for {Options.Name}"));
            var validation = Options.Validator.Invoke(Context, token);

            if (!validation.Status) return Task.FromResult(AuthenticateResult.Fail($@"Auth Failed. Error: {validation.Message}"));

            var identity = new ClaimsIdentity(validation.Claims, this.Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, this.Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
