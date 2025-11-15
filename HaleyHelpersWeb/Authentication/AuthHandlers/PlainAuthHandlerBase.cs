using Haley.Abstractions;
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
        protected virtual Task<IFeedback<string>> GetToken() => GetTokenInternal(Options.Key);
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

                var tokenObj = await GetToken();
                if (!tokenObj.Status) {
                    message += $@"Unable to find a {AuthMode.ToString()} with name {Options.Key}";
                    Logger?.LogError(message);
                    return AuthenticateResult.Fail(message);
                }

                if (string.IsNullOrWhiteSpace(tokenObj.Result)) {
                    message += $@"{AuthMode.ToString()} token value cannot be null or empty for {Options.Key}";
                    Logger?.LogError(message);
                    return AuthenticateResult.Fail(message);
                }

                if (Options.Validator != null) {
                    var validation = await Options.Validator.Invoke(Context, tokenObj.Result, Logger);
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
                    return await Validator.Invoke(Context, tokenObj.Result, Logger);
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

        bool ModeNeedsKey(PlainAuthMode mode) {
            switch (mode) {
                case PlainAuthMode.HeaderAuthToken:
                case PlainAuthMode.HeaderApiKey:
                case PlainAuthMode.Cookie:
                case PlainAuthMode.FormToken:
                case PlainAuthMode.QueryToken:
                    return true;
                case PlainAuthMode.AzureSAML:
                    return false;
                default:
                    return false;
            }
        }

        protected async Task<IFeedback<string>> GetTokenInternal(string? key = null) {
            var fb = new Feedback<string>().SetStatus(false);
            if (ModeNeedsKey(AuthMode) && string.IsNullOrWhiteSpace(key)) return fb.SetMessage("Key cannot be empty for fetching the token.");
            string token = string.Empty;
            switch (AuthMode) {
                case PlainAuthMode.HeaderAuthToken:
                if (!(Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                   authHeader.ToString().StartsWith(key, StringComparison.OrdinalIgnoreCase))) return fb;
               token = authHeader.ToString().Substring((key.Length)).Trim(); //to remove the spaces, if any.
                break;
                case PlainAuthMode.HeaderApiKey:
                if (!Request.Headers.TryGetValue(key, out var apiKeyHeaderValues) || string.IsNullOrWhiteSpace(apiKeyHeaderValues.FirstOrDefault())) return fb;
                token = apiKeyHeaderValues.FirstOrDefault()!.Trim();
                break;
                case PlainAuthMode.Cookie:
                if (!Request.Cookies.TryGetValue(key, out var plainCookie) || string.IsNullOrWhiteSpace(plainCookie)) return fb;
                token = plainCookie;
                break;
                case PlainAuthMode.AzureSAML:
                //Remember when Request.Form is accessed it will read the entire body and pareses it as form fields.
                // If the request has multiple form keys with similar names, the post body will be appended.
                if (Request.HasFormContentType) {
                    var form = await Request.ReadFormAsync();
                    if (!form.TryGetValue("SAMLResponse", out var formvalues) || string.IsNullOrWhiteSpace(formvalues.FirstOrDefault())) return fb;
                    token = formvalues.FirstOrDefault()!.Trim();
                }
                break;
                case PlainAuthMode.FormToken:
                if (Request.HasFormContentType) {
                    var form = await Request.ReadFormAsync();
                    if (!form.TryGetValue(key, out var formvalues) || string.IsNullOrWhiteSpace(formvalues.FirstOrDefault())) return fb;
                    token = formvalues.FirstOrDefault()!.Trim();
                }
                break;
                case PlainAuthMode.QueryToken:
                if (!Request.Query.TryGetValue(key, out var queryTokenValues) || string.IsNullOrWhiteSpace(queryTokenValues.FirstOrDefault())) return fb;
                token = queryTokenValues.FirstOrDefault()!.Trim();
                break;
                default:
                break;
            }
            if (!string.IsNullOrWhiteSpace(token)) return fb.SetStatus(true).SetResult(token);
            return fb;
        }
    }
}
