using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Haley.Models {
    public class ApiKeyAuthHandler : AuthenticationHandler<ApiKeyAuthOptions> {
        public ApiKeyAuthHandler(IOptionsMonitor<ApiKeyAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock) {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() {

            if (Options.Validator == null) return Task.FromResult(AuthenticateResult.Fail("Key Validator is missing"));

            if (!this.Request.Headers.TryGetValue(Options.Header, out var apiKeyHeaderValues)) {
                return Task.FromResult(AuthenticateResult.Fail($@"Unable to find a matching header {Options.Header}"));
            }
            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(providedApiKey)) return Task.FromResult(AuthenticateResult.Fail($@"ApiKey cannot be null or empty for the provided header {Options.Header}"));
            var validation = Options.Validator.Invoke(providedApiKey);

            if (!validation.Status) return Task.FromResult(AuthenticateResult.Fail($@"Provided ApiKey for {Options.Header} is not valid."));

            var identity = new ClaimsIdentity(validation.Claims, this.Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, this.Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
