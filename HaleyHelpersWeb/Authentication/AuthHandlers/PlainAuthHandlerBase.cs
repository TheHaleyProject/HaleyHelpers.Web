using Haley.Enums;
using Haley.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Haley.Models {
    public abstract class PlainAuthHandlerBase<T> : AuthenticationHandler<T> where T: PlainAuthOptions,new() {
        public PlainAuthHandlerBase(IOptionsMonitor<T> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder,clock) {
            
        }
        protected abstract bool GetToken(out string token);
        protected abstract PlainAuthMode AuthMode { get; set; }
        protected virtual Func<HttpContext, string, ILogger, Task<AuthenticateResult>>? Validator { get; set; }
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync() {
            try {
                var endpoint = Context?.GetEndpoint();

                // No endpoint resolved (static files, 404s, etc.) → skip
                if (endpoint is null) return AuthenticateResult.NoResult();

                // Explicitly allow anonymous → skip
                if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null) return AuthenticateResult.NoResult();

                // If endpoint doesn't require authorization → skip
                // Works for Controllers with [Authorize] and Minimal APIs with .RequireAuthorization()
                var authzData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
                if (authzData == null || authzData.Count == 0) return AuthenticateResult.NoResult();

                var callID = RandomUtils.GetString(32).SanitizeBase64();
                string reqIP = string.Empty;
                if (Context != null) {
                    reqIP = WebAuthUtils.GetClientIP(Context);
                }

                string message = $@"Call Id : {callID} | IP : {reqIP} | ";
                if (endpoint is RouteEndpoint re) {
                    message += $@"Endpoint : {re.RoutePattern?.RawText ?? endpoint.ToString()}";
                } else {
                    message += $@"Endpoint : {endpoint}";
                }
                message += Environment.NewLine;

                if (Options.Validator == null && Validator == null) {
                    message += "Auth validator is missing";
                    Logger?.LogError(message);
                    return AuthenticateResult.Fail(message);
                }

                if (!GetToken(out var token)) {
                    message += $@"Unable to find a {AuthMode.ToString()} with name {Options.Key}";
                    Logger?.LogError(message);
                    return AuthenticateResult.Fail(message);
                }

                if (string.IsNullOrWhiteSpace(token)) {
                    message += $@"{AuthMode.ToString()} token value cannot be null or empty for {Options.Key}";
                    Logger?.LogError(message);
                    return AuthenticateResult.Fail(message);
                }

                if (Options.Validator != null) {
                    var validation = await Options.Validator.Invoke(Context, token, Logger);
                    if (!validation.Status) {
                        message += $@"Auth Failed. Error: {validation.Message}";
                        Logger?.LogError(message);
                        return AuthenticateResult.Fail(message);
                    }

                    var identity = new ClaimsIdentity(validation.Result, this.Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, this.Scheme.Name);
                    return AuthenticateResult.Success(ticket);
                } else if (Validator != null) {
                    return await Validator.Invoke(Context, token, Logger);
                } else {
                    message += $@"Auth Failed. Error: No validator was found";
                    Logger?.LogError(message);
                    return AuthenticateResult.Fail(message);
                }
            } catch (Exception ex) {
                Logger?.LogError($@"Error: {ex.ToString()}");
                return AuthenticateResult.Fail($@"Exception Occured");
            }
        }
    }
}
