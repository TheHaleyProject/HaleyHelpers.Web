using Haley.Enums;
using Haley.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Haley.Models {
    public abstract class PlainAuthHandlerBase : AuthenticationHandler<PlainAuthOptions> {
        public PlainAuthHandlerBase(IOptionsMonitor<PlainAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder,clock) {
            
        }
        protected abstract bool GetToken(out string token);
        protected abstract PlainAuthMode AuthMode { get; set; }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
            try {
                var callID = RandomUtils.GetString(32).SanitizeBase64();
                string reqIP = string.Empty;
                if (Context != null) {
                    reqIP = WebHelperUtils.GetClientIP(Context);
                }
                string message = $@"Validation Initiated for request from IP : {reqIP}";
                Logger?.LogInformation($@"{callID} : {message}");
                if (Options.Validator == null) {
                    message = "Auth validator is missing";
                    Logger?.LogError($@"{callID} : {message}");
                    return Task.FromResult(AuthenticateResult.Fail($@"{message}"));
                }

                if (!GetToken(out var token)) {
                    message = $@"Unable to find a {AuthMode.ToString()} with name {Options.Name}";
                    Logger?.LogError($@"{callID} : {message}");
                    return Task.FromResult(AuthenticateResult.Fail($@"{message}"));
                }

                if (string.IsNullOrWhiteSpace(token)) {
                    message = $@"{AuthMode.ToString()} token value cannot be null or empty for {Options.Name}";
                    Logger?.LogError($@"{callID} : {message}");
                    return Task.FromResult(AuthenticateResult.Fail($@"{message}"));
                }

                var validation = Options.Validator.Invoke(Context, token, Logger);

                if (!validation.Status) {
                    message = $@"Auth Failed. Error: {validation.Message}";
                    Logger?.LogError($@"{callID} : {message}");
                    return Task.FromResult(AuthenticateResult.Fail($@"{message}"));
                }

                message = $@"Validation Successful";
                Logger?.LogError($@"{callID} : {message}");

                var identity = new ClaimsIdentity(validation.Claims, this.Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, this.Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            } catch (Exception ex) {
                Logger?.LogError($@"Error: {ex.ToString()}");
                return Task.FromResult(AuthenticateResult.Fail($@"Exception Occured"));
            }
        }
    }
}
